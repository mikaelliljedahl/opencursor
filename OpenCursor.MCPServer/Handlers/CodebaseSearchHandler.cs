using OpenCursor.Client.Commands;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenCursor.Client.Handlers
{
    public class CodebaseSearchHandler : IMcpCommandHandler
    {
        public string CommandName => "codebase_search";

        public bool CanHandle(IMcpCommand command) => command is CodebaseSearchCommand;
        
        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not CodebaseSearchCommand searchCmd)
            {
                throw new ArgumentException($"Expected CodebaseSearchCommand, got {command.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace(searchCmd.Query))
            {
                return ("[Codebase Search] Empty query provided.");
                
            }

            Console.WriteLine($"\n[Codebase Search] Searching for: '{searchCmd.Query}'");
            Console.WriteLine($"Search scope: {(searchCmd.TargetDirectories == null ? "(workspace root)" : string.Join(", ", searchCmd.TargetDirectories))}");
            Console.WriteLine("--- Results ---");

            StringBuilder sb = new StringBuilder();

            try
            {
                // Get directories to search
                var searchDirs = searchCmd.TargetDirectories ?? new List<string> { workspaceRoot };

                foreach (var dir in searchDirs)
                {
                    string fullPath = IMcpCommandHandler.GetFullPath(dir, workspaceRoot);
                    if (!Directory.Exists(fullPath))
                    {
                        return $"[Codebase Search] Directory not found: {dir}";
                        continue;
                    }

                    // Search files in directory
                    var files = Directory.EnumerateFiles(fullPath, "*", new System.IO.EnumerationOptions
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
                                .Where(l => l.Line.Contains(searchCmd.Query, StringComparison.OrdinalIgnoreCase));

                            if (matchingLines.Any())
                            {
                                var relativePath = Path.GetRelativePath(workspaceRoot, file);
                                sb.AppendLine($"\nFile: {relativePath}");
                                foreach (var match in matchingLines)
                                {
                                    sb.AppendLine($"Line {match.Number}: {match.Line.Trim()}" +
                                        (match.Line.Length > 80 ? "..." : ""));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"Error searching file {file}: {ex.Message}");
                        }
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return ($"Error during codebase search: {ex.Message}");
                
            }

            

            //Console.WriteLine("--- End of Results ---");
        }


    }
}
