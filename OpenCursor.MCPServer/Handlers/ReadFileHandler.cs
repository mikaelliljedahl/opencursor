using OpenCursor.Client.Commands;
using System;
using System.IO;

namespace OpenCursor.Client.Handlers
{
    public class ReadFileHandler : IMcpCommandHandler
    {

        public string CommandName => "read_file";

        public bool CanHandle(IMcpCommand command) => command is ReadFileCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not ReadFileCommand readCmd)
            {
                throw new ArgumentException($"Expected ReadFileCommand, got {command.GetType().Name}");
            }

            string fullPath = IMcpCommandHandler.GetFullPath(readCmd.RelativePath, workspaceRoot);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[Read File] File not found: {readCmd.RelativePath}");
                return;
            }

            try
            {
                Console.WriteLine($"\n[Read File] Content of: {Path.GetFileName(readCmd.RelativePath)}");
                if (readCmd.ShouldReadEntireFile || readCmd.StartLine == null || readCmd.EndLine == null)
                {
                    Console.WriteLine(File.ReadAllText(fullPath));
                }
                else
                {
                    var lines = File.ReadLines(fullPath)
                        .Skip(readCmd.StartLine.Value - 1)
                        .Take(readCmd.EndLine.Value - readCmd.StartLine.Value + 1);
                    foreach (var line in lines)
                    {
                        Console.WriteLine(line);
                    }
                }
                Console.WriteLine("--- End of Content ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                throw;
            }
        }

    }
}
