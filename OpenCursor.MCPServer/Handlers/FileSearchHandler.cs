using OpenCursor.Client.Commands;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenCursor.Client.Handlers
{
    public class FileSearchHandler : IMcpCommandHandler
    {
        public string CommandName => "file_search";

        public bool CanHandle(IMcpCommand command) => command is FileSearchCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not FileSearchCommand searchCmd)
            {
                throw new ArgumentException($"Expected FileSearchCommand, got {command.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace(searchCmd.Query))
            {
                return "[File Search] Empty query provided.";
                
            }

            Console.WriteLine($"\n[File Search] Looking for files matching: '{searchCmd.Query}'");
            Console.WriteLine("--- Results ---");

            try
            {
                // Search both files and directories
                var searchOptions = new System.IO.EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                };

                var matches = Directory.EnumerateFileSystemEntries(workspaceRoot, "*", searchOptions)
                    .Select(path => Path.GetRelativePath(workspaceRoot, path))
                    .Select(path => new
                    {
                        Path = path,
                        // Calculate fuzzy match score (0-100)
                        Score = CalculateFuzzyMatchScore(searchCmd.Query.ToLower(), Path.GetFileName(path).ToLower())
                    })
                    .Where(x => x.Score > 30) // Minimum score threshold
                    .OrderByDescending(x => x.Score)
                    .Take(20); // Limit to top 20 results

                if (!matches.Any())
                {
                    return "No matches found.";
                }

                StringBuilder sb = new StringBuilder();
                foreach (var match in matches)
                {
                    sb.AppendLine($"{match.Score,3}% - {match.Path}");
                }
                sb.AppendLine("--- End of Results ---");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error during file search: {ex.Message}";
                
            }
            
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
    }
}
