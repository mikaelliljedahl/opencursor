using OpenCursor.Client.Commands;
using System;
using System.IO;

namespace OpenCursor.Client.Handlers
{
    public class ListDirHandler : IMcpCommandHandler
    {
        public string CommandName => "list_dir";

        public bool CanHandle(IMcpCommand command) => command is ListDirCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not ListDirCommand listCmd)
            {
                throw new ArgumentException($"Expected ListDirCommand, got {command.GetType().Name}");
            }

            string fullPath = IMcpCommandHandler.GetFullPath(listCmd.RelativePath, workspaceRoot);
            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine($"[List Directory] Directory not found: {listCmd.RelativePath}");
                return;
            }

            try
            {
                Console.WriteLine($"\n[List Directory] Contents of: {listCmd.RelativePath}");
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing directory: {ex.Message}");
                throw;
            }
        }


    }
}
