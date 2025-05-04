using OpenCursor.Client.Commands;
using System;
using System.IO;
using System.Text;

namespace OpenCursor.Client.Handlers
{
    public class ListDirHandler : IMcpCommandHandler
    {
        public string CommandName => "list_dir";

        public bool CanHandle(IMcpCommand command) => command is ListDirCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not ListDirCommand listCmd)
            {
                throw new ArgumentException($"Expected ListDirCommand, got {command.GetType().Name}");
            }

            string fullPath = IMcpCommandHandler.GetFullPath(listCmd.RelativePath, workspaceRoot);
            if (!Directory.Exists(fullPath))
            {
                return $"[List Directory] Directory not found: {listCmd.RelativePath}";
                
            }
            StringBuilder sb = new StringBuilder();
            try
            {
                
                sb.AppendLine($"\n[List Directory] Contents of: {listCmd.RelativePath}");
                // List Directories
                foreach (var dir in Directory.GetDirectories(fullPath))
                {
                    sb.AppendLine($"[D] {Path.GetFileName(dir)}");
                }
                // List Files
                foreach (var file in Directory.GetFiles(fullPath))
                {
                    sb.AppendLine($"[F] {Path.GetFileName(file)}");
                }
                sb.AppendLine("--- End of Listing ---");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error listing directory: {ex.Message}");
                throw;
            }

            return sb.ToString();
        }


    }
}
