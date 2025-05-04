using OpenCursor.Client.Commands;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenCursor.Client.Handlers
{
    public class GrepSearchHandler : IMcpCommandHandler
    {
        public string CommandName => "grep_search";

        public bool CanHandle(IMcpCommand command) => command is GrepSearchCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not GrepSearchCommand grepCmd)
            {
                throw new ArgumentException($"Expected GrepSearchCommand, got {command.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace(grepCmd.Query))
            {
                return "[Grep Search] Empty query provided.";
                
            }

            Console.WriteLine($"\n[Grep Search] Searching for pattern: '{grepCmd.Query}'");
            Console.WriteLine($"Case Sensitive: {grepCmd.CaseSensitive ?? false}");
            Console.WriteLine($"Include Pattern: {grepCmd.IncludePattern ?? "(none)"}");
            Console.WriteLine($"Exclude Pattern: {grepCmd.ExcludePattern ?? "(none)"}");
            Console.WriteLine("--- Results ---");

            StringBuilder sb = new StringBuilder();

            try
            {
                // Get all files to search
                var filesToSearch = Directory.EnumerateFiles(workspaceRoot, "*", new System.IO.EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                });

                // Apply include/exclude patterns if specified
                if (!string.IsNullOrWhiteSpace(grepCmd.IncludePattern))
                {
                    filesToSearch = filesToSearch.Where(f => 
                        Path.GetRelativePath(workspaceRoot, f).Contains(grepCmd.IncludePattern, 
                            grepCmd.CaseSensitive == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(grepCmd.ExcludePattern))
                {
                    filesToSearch = filesToSearch.Where(f => !
                        Path.GetRelativePath(workspaceRoot, f).Contains(grepCmd.ExcludePattern, 
                            grepCmd.CaseSensitive == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));
                }

                // Prepare regex for searching
                var options = grepCmd.CaseSensitive == true ? RegexOptions.None : RegexOptions.IgnoreCase;
                var regex = new Regex(grepCmd.Query, options);

                foreach (var file in filesToSearch)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(workspaceRoot, file);
                        var lines = File.ReadLines(file).Select((line, index) => new { Line = line, Number = index + 1 });

                        var matches = lines.Where(l => regex.IsMatch(l.Line));
                        if (matches.Any())
                        {
                            sb.AppendLine($"\nFile: {relativePath}");
                            foreach (var match in matches)
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
            catch (Exception ex)
            {
                sb.AppendLine($"Error during grep search: {ex.Message}");
                throw;
            }
            sb.AppendLine("--- End of Results ---");

            return sb.ToString();
        }
    }
}
