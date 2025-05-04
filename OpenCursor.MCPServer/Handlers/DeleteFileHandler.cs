using OpenCursor.Client.Commands;
using System;
using System.IO;

namespace OpenCursor.Client.Handlers
{
    public class DeleteFileHandler : IMcpCommandHandler
    {
        

        public string CommandName => "delete_file";

        public bool CanHandle(IMcpCommand command) => command is DeleteFileCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not DeleteFileCommand deleteCmd)
            {
                throw new ArgumentException($"Expected DeleteFileCommand, got {command.GetType().Name}");
            }

            string fullPath = IMcpCommandHandler.GetFullPath(deleteCmd.TargetFile, workspaceRoot);
            if (!File.Exists(fullPath))
            {
                return $"[Delete File] File not found: {deleteCmd.TargetFile}";
                
            }

            try
            {
                File.Delete(fullPath);
                return $"\n[Delete File] Successfully deleted: {Path.GetRelativePath(workspaceRoot, fullPath)}";
            }
            catch (Exception ex)
            {
                return $"Error deleting file: {ex.Message}";
                
            }
        }

    }
}
