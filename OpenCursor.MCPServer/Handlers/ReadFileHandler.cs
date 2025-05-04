using OpenCursor.Client.Commands;
using System;
using System.IO;
using System.Text;

namespace OpenCursor.Client.Handlers
{
    public class ReadFileHandler : IMcpCommandHandler
    {

        public string CommandName => "read_file";

        public bool CanHandle(IMcpCommand command) => command is ReadFileCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not ReadFileCommand readCmd)
            {
                throw new ArgumentException($"Expected ReadFileCommand, got {command.GetType().Name}");
            }

            string fullPath = IMcpCommandHandler.GetFullPath(readCmd.RelativePath, workspaceRoot);
            if (!File.Exists(fullPath))
            {
                return $"[Read File] File not found: {readCmd.RelativePath}";
            }
            try
            {
                Console.WriteLine($"\n[Read File] Content of: {Path.GetFileName(readCmd.RelativePath)}");
                if (readCmd.ShouldReadEntireFile || readCmd.StartLine == null || readCmd.EndLine == null)
                {
                    return await File.ReadAllTextAsync(fullPath);
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    var lines = File.ReadLines(fullPath)
                        .Skip(readCmd.StartLine.Value - 1)
                        .Take(readCmd.EndLine.Value - readCmd.StartLine.Value + 1);
                    foreach (var line in lines)
                    {
                        sb.AppendLine(line);
                        
                    }
                    sb.AppendLine("--- End of Content ---");
                    return sb.ToString();
                }
                

            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
                
            }
        }

    }
}
