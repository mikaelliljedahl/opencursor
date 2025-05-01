using OpenCursor.Client.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics; // Required for Process

namespace OpenCursor.Client
{
    public class McpProcessor
    {
        private string _workspaceRoot;

        public McpProcessor(string workspaceRoot)
        {
            _workspaceRoot = workspaceRoot;
            if (!Directory.Exists(_workspaceRoot))
            {
                Directory.CreateDirectory(_workspaceRoot);
                Console.WriteLine($"Workspace root created: {_workspaceRoot}");
            }
        }

        public async Task UpdateWorkspaceRoot(string newDirectory)
        {
            _workspaceRoot = newDirectory;
        }

        // Central method to apply a list of commands
        public async Task ApplyMcpCommands(IEnumerable<IMcpCommand> commands, string currentDirectory)
        {
            if (commands == null || !commands.Any())
            {
                Console.WriteLine("No commands to apply.");
                return;
            }

            Console.WriteLine("\n--- Proposed Actions ---");
            // First, display all proposed actions
            foreach (var command in commands)
            {
                DisplayProposedAction(command);
            }
            Console.WriteLine("----------------------");

            // Then, execute with confirmation where needed
            foreach (var command in commands)
            {
                bool confirmed = true; // Assume confirmed unless confirmation is needed
                string confirmationPrompt = GetConfirmationPrompt(command);

                if (!string.IsNullOrEmpty(confirmationPrompt))
                {
                    confirmed = RequestUserConfirmation(confirmationPrompt);
                }

                if (confirmed)
                {
                    Console.WriteLine($"Executing: {GetCommandDescription(command)}...");
                    try
                    {
                        await ExecuteCommand(command); // Now await the execution
                        Console.WriteLine("Execution successful.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Execution failed: {ex.Message}");
                        // Optionally log the full exception details
                    }
                }
                else
                {
                    Console.WriteLine($"Skipped: {GetCommandDescription(command)} (User cancelled).");
                }
                Console.WriteLine(); // Add a blank line for readability
            }
        }

        // Helper to display the proposed action
        private void DisplayProposedAction(IMcpCommand command)
        {
            Console.WriteLine($"- {GetCommandDescription(command)}");
        }

        // Helper to get a description for display/logging
        private string GetCommandDescription(IMcpCommand command)
        {
            return command switch
            {
                CreateFileCommand c => $"Create file '{c.RelativePath}'",
                UpdateFileCommand u => $"Update file '{u.RelativePath}'",
                DeleteFileCommand d => $"Delete file '{d.TargetFile}'",
                ReadFileCommand r => $"Read file '{r.RelativePath}'" + (r.ShouldReadEntireFile ? " (entire)" : $" (lines {r.StartLine}-{r.EndLine})"),
                CodebaseSearchCommand cs => $"Search codebase: '{cs.Query}'" + (cs.TargetDirectories?.Any() == true ? $" in {string.Join(", ", cs.TargetDirectories)}" : ""),
                RunTerminalCommand rt => $"Run command: '{rt.CommandLine}'" + (rt.IsBackground ? " (background)" : ""),
                ListDirCommand ld => $"List directory '{ld.RelativePath}'",
                GrepSearchCommand gs => $"Grep search: '{gs.Query}'",
                FileSearchCommand fs => $"File search: '{fs.Query}'",
                EditFileCommand ef => $"Edit file '{ef.TargetFile}' with instructions: '{Shorten(ef.Instructions, 50)}'",
                ReapplyCommand ra => $"Reapply changes to '{ra.TargetFile}'",
                ParallelApplyCommand pa => $"Apply parallel edit plan: '{Shorten(pa.EditPlan, 50)}' to {pa.EditRegions.Count} regions",
                _ => $"Unknown command type: {command.GetType().Name}"
            };
        }

        // Helper to determine the confirmation prompt message (returns null if no confirmation needed)
        private string? GetConfirmationPrompt(IMcpCommand command)
        {
            return command switch
            {
                CreateFileCommand _ => "Create this file?",
                UpdateFileCommand _ => "Update this file?",
                DeleteFileCommand _ => "DELETE this file?",
                EditFileCommand _ => "Apply edits to this file?",
                RunTerminalCommand _ => "Run this terminal command?",
                ReapplyCommand _ => "Reapply changes to this file?",
                ParallelApplyCommand _ => "Apply these parallel edits?",
                _ => null // No confirmation needed for read, list, search
            };
        }

        // Helper method to get user confirmation
        private bool RequestUserConfirmation(string promptMessage)
        {
            Console.Write($"ACTION REQUIRED: {promptMessage} (Y / Enter to confirm, any other key to cancel): ");
            var key = Console.ReadKey(intercept: true); // Read key without displaying it
            bool confirmed = key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter;
            Console.WriteLine(confirmed ? "Confirmed." : "Cancelled."); // Provide feedback
            return confirmed;
        }


        // Main execution logic dispatcher
        private async Task ExecuteCommand(IMcpCommand command)
        {
            string fullPath;
            switch (command)
            {
                case CreateFileCommand c:
                    fullPath = GetFullPath(c.RelativePath);
                    ExecuteCreateFile(fullPath, c.Content);
                    break;

                case UpdateFileCommand u:
                    fullPath = GetFullPath(u.RelativePath);
                    ExecuteUpdateFile(fullPath, u.Content);
                    break;

                case DeleteFileCommand d:
                    fullPath = GetFullPath(d.TargetFile); // Use TargetFile now
                    ExecuteDeleteFile(fullPath);
                    break;

                case ReadFileCommand r:
                    fullPath = GetFullPath(r.RelativePath);
                    ExecuteReadFile(fullPath, r.ShouldReadEntireFile, r.StartLine, r.EndLine);
                    break;
                
                case ListDirCommand ld:
                    fullPath = GetFullPath(ld.RelativePath);
                    ExecuteListDir(fullPath);
                    break;
                
                case RunTerminalCommand rt:
                    await ExecuteRunTerminal(rt.CommandLine, rt.IsBackground);
                    break;

                case CodebaseSearchCommand cs:
                    ExecuteCodebaseSearch(cs.Query, cs.TargetDirectories);
                    break;

                case GrepSearchCommand gs:
                    ExecuteGrepSearch(gs.Query, gs.CaseSensitive, gs.IncludePattern, gs.ExcludePattern);
                    break;

                case FileSearchCommand fs:
                    ExecuteFileSearch(fs.Query);
                    break;
                
                case EditFileCommand ef:
                    fullPath = GetFullPath(ef.TargetFile);
                    ExecuteEditFile(fullPath, ef.Instructions, ef.CodeEdit);
                    break;

                case ReapplyCommand ra:
                     fullPath = GetFullPath(ra.TargetFile);
                    ExecuteReapply(fullPath);
                    break;

                 case ParallelApplyCommand pa:
                    ExecuteParallelApply(pa.EditPlan, pa.EditRegions);
                    break;

                default:
                    Console.WriteLine($"Warning: No execution logic defined for command type {command.GetType().Name}");
                    break;
            }
        }

        // --- Individual Command Execution Methods ---

        private void ExecuteCreateFile(string fullPath, string content)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, content ?? string.Empty); // Handle null content
                // Console.WriteLine($"File created: {fullPath}"); // Moved confirmation up
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Error creating file {fullPath}: {ex.Message}"); // Moved confirmation up
                 throw; // Re-throw to be caught by the main loop
            }
        }

        private void ExecuteUpdateFile(string fullPath, string content)
        {
             try
            {
                if (!File.Exists(fullPath)) throw new FileNotFoundException("File not found for update.", fullPath);
                File.WriteAllText(fullPath, content ?? string.Empty); // Overwrites existing file
                // Console.WriteLine($"File updated: {fullPath}"); // Moved confirmation up
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Error updating file {fullPath}: {ex.Message}"); // Moved confirmation up
                 throw; // Re-throw
            }
        }

        private void ExecuteDeleteFile(string fullPath)
        {
             try
            {
                if (!File.Exists(fullPath)) throw new FileNotFoundException("File not found for delete.", fullPath);
                File.Delete(fullPath);
                // Console.WriteLine($"File deleted: {fullPath}"); // Moved confirmation up
            }
            catch (Exception ex)
            {
                 // Console.WriteLine($"Error deleting file {fullPath}: {ex.Message}"); // Moved confirmation up
                 throw; // Re-throw
            }
        }

        private void ExecuteReadFile(string fullPath, bool readAll, int? startLine, int? endLine)
        {
            if (!File.Exists(fullPath)) throw new FileNotFoundException("File not found for reading.", fullPath);
            
            Console.WriteLine($"--- Content of {Path.GetFileName(fullPath)} ---");
            if (readAll || startLine == null || endLine == null)
            {
                Console.WriteLine(File.ReadAllText(fullPath));
            }
            else
            {
                var lines = File.ReadLines(fullPath).Skip(startLine.Value - 1).Take(endLine.Value - startLine.Value + 1);
                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                }
            }
             Console.WriteLine("--- End of Content ---");
        }

         private void ExecuteListDir(string fullPath)
        {
            if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException($"Directory not found: {fullPath}");

            Console.WriteLine($"--- Contents of {fullPath} ---");
            // List Directories
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                Console.WriteLine($"[D] {Path.GetFileName(dir)}");
            }
            // List Files
            foreach (var file in Directory.GetFiles(fullPath))
            {
                 Console.WriteLine($"[F] {Path.GetFileName(file)}");
            }
            Console.WriteLine("--- End of Listing ---");
        }

        private async Task ExecuteRunTerminal(string commandLine, bool isBackground)
        {
            Console.WriteLine($"Attempting to run: {commandLine}");
            // Basic implementation - Consider security implications carefully!
            // This runs commands in the context of the OpenCursor.Client process.
            // For more robust execution, consider specific shells (cmd, powershell) 
            // and argument escaping.
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe", // Or powershell.exe
                    Arguments = $"/C {commandLine}", // /C executes command and terminates
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _workspaceRoot // Run in the workspace root
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    
                    // Capture output/error streams
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    if (!isBackground) // Only wait if not background
                    {
                        await process.WaitForExitAsync(); // Wait for the process to complete
                        Console.WriteLine("--- Command Output ---");
                        if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);
                        if (!string.IsNullOrWhiteSpace(error)) Console.Error.WriteLine(error); // Write errors to stderr
                        Console.WriteLine($"--- Command Finished (Exit Code: {process.ExitCode}) ---");
                        if(process.ExitCode != 0) throw new Exception($"Command failed with exit code {process.ExitCode}");
                    }
                    else
                    {
                        Console.WriteLine($"Command started in background (PID: {process.Id})");
                        // Don't wait, don't capture output synchronously for background tasks
                    }
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Failed to run command '{commandLine}': {ex.Message}");
                throw;
            }
        }

        // --- Placeholder Methods for Complex Commands ---
        private void ExecuteCodebaseSearch(string query, List<string>? targetDirs)
        {
             Console.WriteLine($"[Placeholder] Codebase search for '{query}' in directories: {(targetDirs == null ? "(workspace root)" : string.Join(", ", targetDirs))}");
             // TODO: Implement actual semantic search logic
        }

        private void ExecuteGrepSearch(string query, bool? caseSensitive, string? includePattern, string? excludePattern)
        {
            Console.WriteLine($"[Placeholder] Grep search for '{query}' (CaseSensitive={caseSensitive}, Include='{includePattern}', Exclude='{excludePattern}')");
            // TODO: Implement file content searching (e.g., basic string search or Regex)
        }

        private void ExecuteFileSearch(string query)
        {
             Console.WriteLine($"[Placeholder] File search (fuzzy find) for '{query}'");
             // TODO: Implement file system traversal and matching logic
        }

        private void ExecuteEditFile(string fullPath, string instructions, string codeEdit)
        {
            Console.WriteLine($"[Placeholder] Edit file '{fullPath}' with instructions: '{Shorten(instructions, 100)}'. Code Edit Preview: '{Shorten(codeEdit.Replace("\n", "\\n"), 100)}'");
            // TODO: Implement complex file editing logic (e.g., applying diff/patch)
            // For now, maybe just overwrite or append?
            // File.WriteAllText(fullPath, codeEdit); // Simplistic overwrite - DANGEROUS without proper logic
             Console.WriteLine("Warning: Actual file edit not implemented yet.");
             // throw new NotImplementedException("File editing logic not implemented.");
        }
        
        private void ExecuteReapply(string fullPath)
        {
            Console.WriteLine($"[Placeholder] Reapply changes to '{fullPath}'");
            // TODO: Implement reapply logic (likely needs state tracking of previous edits)
            Console.WriteLine("Warning: Reapply logic not implemented yet.");
            // throw new NotImplementedException("Reapply logic not implemented.");
        }

        private void ExecuteParallelApply(string editPlan, List<EditRegion> editRegions)
        {
            Console.WriteLine($"[Placeholder] Parallel apply edit plan: '{Shorten(editPlan, 100)}' to {editRegions.Count} regions:");
            foreach(var region in editRegions)
            {
                 Console.WriteLine($"  - Region: {region.RelativeWorkspacePath} (Lines {region.StartLine}-{region.EndLine})");
            }
             // TODO: Implement parallel editing logic across multiple files/regions
            Console.WriteLine("Warning: Parallel apply logic not implemented yet.");
            // throw new NotImplementedException("Parallel apply logic not implemented.");
        }

        // --- Utility Methods ---

        private string GetFullPath(string relativePath)
        {
            // Basic path combination, consider normalization and security checks
            string combinedPath = Path.Combine(_workspaceRoot, relativePath);
            return Path.GetFullPath(combinedPath); // Resolves relative segments like '..'
        }

        private static string Shorten(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text ?? string.Empty;
            return text.Substring(0, maxLength) + "...";
        }
    }
}
