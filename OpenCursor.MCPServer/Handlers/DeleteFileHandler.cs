using OpenCursor.Client.Commands;
using System;
using System.IO;

namespace OpenCursor.Client.Handlers
{
    public class DeleteFileHandler : IMcpCommandHandler
    {
        private readonly string workspaceRoot;

        public DeleteFileHandler(string workspaceRoot)
        {
            workspaceRoot = workspaceRoot;
        }

        public string CommandName => "delete_file";

        public bool CanHandle(IMcpCommand command) => command is DeleteFileCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not DeleteFileCommand deleteCmd)
            {
                throw new ArgumentException($"Expected DeleteFileCommand, got {command.GetType().Name}");
            }

            string fullPath = IMcpCommandHandler.GetFullPath(deleteCmd.TargetFile, workspaceRoot);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[Delete File] File not found: {deleteCmd.TargetFile}");
                return;
            }

            try
            {
                File.Delete(fullPath);
                Console.WriteLine($"\n[Delete File] Successfully deleted: {Path.GetRelativePath(workspaceRoot, fullPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                throw;
            }
        }

    }
}
