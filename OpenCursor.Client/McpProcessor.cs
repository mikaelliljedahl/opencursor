using System.Text;
using System.Text.RegularExpressions;

namespace OpenCursor.Client;

public static class McpProcessor
{
    private static readonly Regex CommandRegex = new(@"^@(\w+)_FILE\s+(.+)", RegexOptions.Multiline);

    public static void ApplyMcpCommands(string mcpText, string rootPath)
    {
        var matches = CommandRegex.Matches(mcpText);

        if (matches.Count == 0)
        {
            Console.WriteLine("No MCP commands found.");
            return;
        }

        foreach (Match match in matches)
        {
            var action = match.Groups[1].Value.ToUpperInvariant();
            var relativePath = match.Groups[2].Value.Trim();

            var start = match.Index + match.Length;
            var end = mcpText.IndexOf("@END", start, StringComparison.OrdinalIgnoreCase);

            string content = "";
            if (end > start)
            {
                content = mcpText.Substring(start, end - start).Trim();
            }

            var fullPath = Path.Combine(rootPath, relativePath);

            switch (action)
            {
                case "CREATE":
                    CreateFile(fullPath, content);
                    break;
                case "UPDATE":
                    UpdateFile(fullPath, content);
                    break;
                case "DELETE":
                    DeleteFile(fullPath);
                    break;
                default:
                    Console.WriteLine($"Unknown MCP action: {action}");
                    break;
            }
        }
    }

    private static void CreateFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir!);
        }

        File.WriteAllText(path, content);
        Console.WriteLine($"Created: {path}");
    }

    private static void UpdateFile(string path, string content)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Cannot update missing file: {path}");
            return;
        }

        File.WriteAllText(path, content);
        Console.WriteLine($"Updated: {path}");
    }

    private static void DeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Cannot delete missing file: {path}");
            return;
        }

        File.Delete(path);
        Console.WriteLine($"Deleted: {path}");
    }
}
