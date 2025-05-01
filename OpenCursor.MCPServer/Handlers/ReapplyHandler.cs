using OpenCursor.Client.Commands;
using System;
using System.IO;

namespace OpenCursor.Client.Handlers
{
    public class ReapplyHandler : IMcpCommandHandler
    {
        public string CommandName => "reapply";

        public bool CanHandle(IMcpCommand command) => command is ReapplyCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not ReapplyCommand reapplyCmd)
            {
                throw new ArgumentException($"Expected ReapplyCommand, got {command.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace(reapplyCmd.TargetFile))
            {
                Console.WriteLine("[Reapply] Missing target file path.");
                return;
            }

            try
            {
                // Check for a backup file
                string fullPath = IMcpCommandHandler.GetFullPath(reapplyCmd.TargetFile, workspaceRoot);
                string backupPath = fullPath + ".bak";
                if (!File.Exists(backupPath))
                {
                    Console.WriteLine($"[Reapply] No backup file found for: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                    return;
                }

                // Read both files
                string currentContent = File.ReadAllText(fullPath);
                string backupContent = File.ReadAllText(backupPath);

                // If the files are different, restore from backup
                if (currentContent != backupContent)
                {
                    File.Copy(backupPath, fullPath, true);
                    Console.WriteLine($"\n[Reapply] Restored: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                    Console.WriteLine($"Backup file: {Path.GetRelativePath(workspaceRoot, backupPath)}");

                    // Show a preview of the changes
                    Console.WriteLine("\n--- Changes ---");
                    Console.WriteLine($"Current lines: {currentContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                    Console.WriteLine($"Backup lines: {backupContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                    Console.WriteLine("--- End of Changes ---");
                }
                else
                {
                    Console.WriteLine($"[Reapply] No changes to restore for: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during reapply: {ex.Message}");
                throw;
            }
        }

    }
}
