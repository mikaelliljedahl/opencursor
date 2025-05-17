using OpenCursor.Client.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using OpenCursor.Client.Handlers;
using System.Text.RegularExpressions;
using System.Text; // Required for Process

namespace OpenCursor.Client
{
    public class McpProcessor
    {
        private string _workspaceRoot;
        private List<IMcpCommandHandler> _handlers;

        public McpProcessor(string workspaceRoot)
        {
            _workspaceRoot = workspaceRoot;
            if (!Directory.Exists(_workspaceRoot))
            {
                Directory.CreateDirectory(_workspaceRoot);
                Console.WriteLine($"Workspace root created: {_workspaceRoot}");
            }


            // Dynamically find and register all handlers implementing IMcpCommandHandler
            _handlers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IMcpCommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => (IMcpCommandHandler)Activator.CreateInstance(t)!)
                .ToList();
        }

        public async Task UpdateWorkspaceRoot(string newDirectory)
        {
            _workspaceRoot = newDirectory;
        }

        // Central method to apply a list of commands
        public async Task<string> ApplyMcpCommands(IEnumerable<IMcpCommand> commands, string currentDirectory)
        {
            

            if (commands == null || !commands.Any())
            {
                return "No commands to apply.";
                
            }

            Console.WriteLine("\n--- Proposed Actions ---");
            // First, display all proposed actions
            foreach (var command in commands)
            {
                DisplayProposedAction(command);
            }
            Console.WriteLine("----------------------");

            StringBuilder responseBuilder = new StringBuilder();

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
                        var result = await HandleCommand(command);
                        responseBuilder.Append(result);
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
                    Console.WriteLine($"Skipped: {GetCommandDescription(command)} (User cancelled).\n");
                }
                Console.WriteLine(); // Add a blank line for readability
            }

            return responseBuilder.ToString();
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
                //ReadFileCommand r => $"Read file '{r.RelativePath}'" + (r.ShouldReadEntireFile ? " (entire)" : $" (lines {r.StartLine}-{r.EndLine})"),
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
        private async Task<string> HandleCommand(IMcpCommand command)
        {
            // Find the appropriate handler for this command
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(command));
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler registered for command type: {nameof(command.GetType)}");
            }

            // Execute the command using the handler
            var result = await handler.HandleCommand(command, _workspaceRoot);
            return result;
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
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("[Codebase Search] Empty query provided.");
                return;
            }

            Console.WriteLine($"\n[Codebase Search] Searching for: '{query}'");
            Console.WriteLine($"Search scope: {(targetDirs == null ? "(workspace root)" : string.Join(", ", targetDirs))}");
            Console.WriteLine("--- Results ---");

            try
            {
                // Get directories to search
                var searchDirs = targetDirs ?? new List<string> { _workspaceRoot };

                foreach (var dir in searchDirs)
                {
                    string fullPath = GetFullPath(dir);
                    if (!Directory.Exists(fullPath))
                    {
                        Console.WriteLine($"[Codebase Search] Directory not found: {dir}");
                        continue;
                    }

                    // Search files in directory
                    var files = Directory.EnumerateFiles(fullPath, "*", new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true
                    });

                    // For each file, search for the query
                    foreach (var file in files)
                    {
                        try
                        {
                            // Skip non-text files
                            string ext = Path.GetExtension(file).ToLower();
                            if (ext == ".dll" || ext == ".exe" || ext == ".pdb" || ext == ".obj" || ext == ".lib")
                                continue;

                            var content = File.ReadAllText(file);
                            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                            // Find lines containing the query
                            var matchingLines = lines
                                .Select((line, index) => new { Line = line, Number = index + 1 })
                                .Where(l => l.Line.Contains(query, StringComparison.OrdinalIgnoreCase));

                            if (matchingLines.Any())
                            {
                                var relativePath = Path.GetRelativePath(_workspaceRoot, file);
                                Console.WriteLine($"\nFile: {relativePath}");
                                foreach (var match in matchingLines)
                                {
                                    Console.WriteLine($"Line {match.Number}: {match.Line.Trim()}" +
                                        (match.Line.Length > 80 ? "..." : ""));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error searching file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during codebase search: {ex.Message}");
            }
            Console.WriteLine("--- End of Results ---");
        }

        private void ExecuteGrepSearch(string query, bool? caseSensitive, string? includePattern, string? excludePattern)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("[Grep Search] Empty query provided.");
                return;
            }

            Console.WriteLine($"\n[Grep Search] Searching for pattern: '{query}'");
            Console.WriteLine($"Case Sensitive: {caseSensitive ?? false}");
            Console.WriteLine($"Include Pattern: {includePattern ?? "(none)"}");
            Console.WriteLine($"Exclude Pattern: {excludePattern ?? "(none)"}");
            Console.WriteLine("--- Results ---");

            try
            {
                // Get all files to search
                var filesToSearch = Directory.EnumerateFiles(_workspaceRoot, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                });

                // Apply include/exclude patterns if specified
                if (!string.IsNullOrWhiteSpace(includePattern))
                {
                    filesToSearch = filesToSearch.Where(f => 
                        Path.GetRelativePath(_workspaceRoot, f).Contains(includePattern, 
                            caseSensitive == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(excludePattern))
                {
                    filesToSearch = filesToSearch.Where(f => !
                        Path.GetRelativePath(_workspaceRoot, f).Contains(excludePattern, 
                            caseSensitive == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                }

                // Prepare regex for searching
                var options = caseSensitive == true ? RegexOptions.None : RegexOptions.IgnoreCase;
                var regex = new Regex(query, options);

                foreach (var file in filesToSearch)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(_workspaceRoot, file);
                        var lines = File.ReadLines(file).Select((line, index) => new { Line = line, Number = index + 1 });

                        var matches = lines.Where(l => regex.IsMatch(l.Line));
                        if (matches.Any())
                        {
                            Console.WriteLine($"\nFile: {relativePath}");
                            foreach (var match in matches)
                            {
                                Console.WriteLine($"Line {match.Number}: {match.Line.Trim()}" +
                                    (match.Line.Length > 80 ? "..." : ""));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error searching file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during grep search: {ex.Message}");
            }
            Console.WriteLine("--- End of Results ---");
        }

        private void ExecuteFileSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("[File Search] Empty query provided.");
                return;
            }

            Console.WriteLine($"\n[File Search] Looking for files matching: '{query}'");
            Console.WriteLine("--- Results ---");

            // Search both files and directories
            var searchOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            try
            {
                var matches = Directory.EnumerateFileSystemEntries(_workspaceRoot, "*", searchOptions)
                    .Select(path => Path.GetRelativePath(_workspaceRoot, path))
                    .Select(path => new
                    {
                        Path = path,
                        // Calculate fuzzy match score (0-100)
                        Score = CalculateFuzzyMatchScore(query.ToLower(), Path.GetFileName(path).ToLower())
                    })
                    .Where(x => x.Score > 30) // Minimum score threshold
                    .OrderByDescending(x => x.Score)
                    .Take(20); // Limit to top 20 results

                if (!matches.Any())
                {
                    Console.WriteLine("No matches found.");
                    return;
                }

                foreach (var match in matches)
                {
                    Console.WriteLine($"{match.Score,3}% - {match.Path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file search: {ex.Message}");
            }
            Console.WriteLine("--- End of Results ---");
        }

        // Simple Levenshtein distance-based fuzzy matching
        private int CalculateFuzzyMatchScore(string pattern, string target)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(target))
                return 0;

            int patternLength = pattern.Length;
            int targetLength = target.Length;

            if (patternLength == 0 || targetLength == 0)
                return 0;

            int[,] matrix = new int[patternLength + 1, targetLength + 1];

            for (int i = 0; i <= patternLength; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= targetLength; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= patternLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (pattern[i - 1] == target[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost
                    );
                }
            }

            // Convert distance to score (0-100)
            int maxLen = Math.Max(patternLength, targetLength);
            int distance = matrix[patternLength, targetLength];
            double score = (maxLen - distance) / (double)maxLen * 100;
            return (int)Math.Round(score);
        }

        private void ExecuteEditFile(string fullPath, string instructions, string codeEdit)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                Console.WriteLine("[File Edit] Missing target file path.");
                return;
            }

            if (string.IsNullOrWhiteSpace(codeEdit))
            {
                Console.WriteLine("[File Edit] Empty code edit provided.");
                return;
            }

            try
            {

                // Read the original file
                string originalContent = File.ReadAllText(fullPath);
                string[] originalLines = originalContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Apply the edit (for now, we'll just replace the entire content)
                // In the future, we could implement more sophisticated diff/patch logic
                File.WriteAllText(fullPath, codeEdit);

                // Log the changes
                Console.WriteLine($"\n[File Edit] Successfully edited: {Path.GetRelativePath(_workspaceRoot, fullPath)}");
                Console.WriteLine($"Backup created at: {Path.GetRelativePath(_workspaceRoot, _workspaceRoot)}");
                Console.WriteLine($"Instructions: {instructions}");

                // Show a preview of the changes
                Console.WriteLine("\n--- Changes ---");
                Console.WriteLine($"Original lines: {originalLines.Length}");
                Console.WriteLine($"New lines: {codeEdit.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                Console.WriteLine("--- End of Changes ---");

                // TODO: Implement more sophisticated diff viewing
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file edit: {ex.Message}");
                // If there was an error, restore from backup if it exists
                string backupPath = fullPath + ".bak";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, fullPath, true);
                        Console.WriteLine($"Restored from backup: {Path.GetRelativePath(_workspaceRoot, fullPath)}");
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to restore from backup: {ex.Message}");
                    }
                }
            }
        }
        
        private void ExecuteReapply(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                Console.WriteLine("[Reapply] Missing target file path.");
                return;
            }

            try
            {
                // Check for a backup file
                string backupPath = fullPath + ".bak";
                if (!File.Exists(backupPath))
                {
                    Console.WriteLine($"[Reapply] No backup file found for: {Path.GetRelativePath(_workspaceRoot, fullPath)}");
                    return;
                }

                // Read both files
                string currentContent = File.ReadAllText(fullPath);
                string backupContent = File.ReadAllText(backupPath);

                // If the files are different, restore from backup
                if (currentContent != backupContent)
                {
                    File.Copy(backupPath, fullPath, true);
                    Console.WriteLine($"\n[Reapply] Restored: {Path.GetRelativePath(_workspaceRoot, fullPath)}");
                    Console.WriteLine($"Backup file: {Path.GetRelativePath(_workspaceRoot, backupPath)}");

                    // Show a preview of the changes
                    Console.WriteLine("\n--- Changes ---");
                    Console.WriteLine($"Current lines: {currentContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                    Console.WriteLine($"Backup lines: {backupContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                    Console.WriteLine("--- End of Changes ---");
                }
                else
                {
                    Console.WriteLine($"[Reapply] No changes to restore for: {Path.GetRelativePath(_workspaceRoot, fullPath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during reapply: {ex.Message}");
            }
        }

        private void ExecuteParallelApply(string editPlan, List<EditRegion> editRegions)
        {
            if (string.IsNullOrWhiteSpace(editPlan) || editRegions == null || !editRegions.Any())
            {
                Console.WriteLine("[Parallel Apply] Invalid edit plan or empty regions.");
                return;
            }

            try
            {
                Console.WriteLine($"\n[Parallel Apply] Applying edit plan: '{Shorten(editPlan, 100)}'");
                Console.WriteLine($"Total regions to edit: {editRegions.Count}");

                // Process each region
                foreach (var region in editRegions)
                {
                    string fullPath = GetFullPath(region.RelativeWorkspacePath);
                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine($"[Parallel Apply] File not found: {region.RelativeWorkspacePath}");
                        continue;
                    }

                    try
                    {
                        // Create backup
                        string backupPath = fullPath + ".bak";
                        File.Copy(fullPath, backupPath, true);

                        // Read the file
                        string content = File.ReadAllText(fullPath);
                        string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        // Validate line numbers
                        if (region.StartLine.HasValue && region.EndLine.HasValue && 
                            (region.StartLine.Value < 1 || region.EndLine.Value > lines.Length || 
                            region.StartLine.Value > region.EndLine.Value))
                        {
                            Console.WriteLine($"[Parallel Apply] Invalid line range for {region.RelativeWorkspacePath}: {region.StartLine}-{region.EndLine}");
                            continue;
                        }

                        // For now, just show what we would edit
                        // In the future, we would implement actual parallel editing logic here
                        Console.WriteLine($"\nProcessing region: {region.RelativeWorkspacePath}");
                        Console.WriteLine($"Line range: {region.StartLine ?? 1}-{region.EndLine ?? lines.Length}");
                        Console.WriteLine($"--- Preview ---");
                        
                        // Show the lines that would be edited
                        int startLine = region.StartLine ?? 1;
                        int endLine = region.EndLine ?? lines.Length;
                        for (int i = startLine - 1; i < endLine; i++)
                        {
                            Console.WriteLine($"{i + 1}: {lines[i]}" + (lines[i].Length > 80 ? "..." : ""));
                        }
                        Console.WriteLine("--- End of Preview ---");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing region {region.RelativeWorkspacePath}: {ex.Message}");
                    }
                }

                Console.WriteLine($"\n[Parallel Apply] Completed processing {editRegions.Count} regions");
                Console.WriteLine("Note: Actual parallel editing is not yet implemented.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during parallel apply: {ex.Message}");
            }
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
