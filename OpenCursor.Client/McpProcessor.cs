using System.Text;
using System.Text.RegularExpressions;
using OpenCursor.Client.Commands;

namespace OpenCursor.Client;

public class McpProcessor
{
    public void ApplyMcpCommands(IEnumerable<IMcpCommand> commands, string rootPath)
    {
        foreach (var command in commands)
        {
            switch (command)
            {
                case CreateFileCommand createFileCmd:
                {
                    var fullPath = Path.Combine(rootPath, createFileCmd.RelativePath);
                    CreateFile(fullPath, createFileCmd.Content ?? string.Empty);
                    break;
                }
                case UpdateFileCommand updateFileCmd:
                {
                    var fullPath = Path.Combine(rootPath, updateFileCmd.RelativePath);
                    UpdateFile(fullPath, updateFileCmd.Content ?? string.Empty);
                    break;
                }
                case DeleteFileCommand deleteFileCmd:
                {
                    var fullPath = Path.Combine(rootPath, deleteFileCmd.RelativePath);
                    DeleteFile(fullPath);
                    break;
                }
                default:
                    Console.WriteLine($"Unknown or unsupported MCP command type: {command.GetType().Name}");
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
