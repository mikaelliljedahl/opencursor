using OpenCursor.Client.Commands;
using System;
using System.IO;
using System.Text;

namespace OpenCursor.Client.Handlers
{
    public class ReapplyHandler : IMcpCommandHandler
    {
        public string CommandName => "reapply";

        public bool CanHandle(IMcpCommand command) => command is ReapplyCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not ReapplyCommand reapplyCmd)
            {
                throw new ArgumentException($"Expected ReapplyCommand, got {command.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace(reapplyCmd.TargetFile))
            {
                return "[Reapply] Missing target file path.";
                
            }

            StringBuilder sb = new StringBuilder();

            try
            {
                // Check for a backup file
                string fullPath = IMcpCommandHandler.GetFullPath(reapplyCmd.TargetFile, workspaceRoot);
                string backupPath = fullPath + ".bak";
                if (!File.Exists(backupPath))
                {
                    return $"[Reapply] No backup file found for: {Path.GetRelativePath(workspaceRoot, fullPath)}";
                    
                }

                // Read both files
                string currentContent = File.ReadAllText(fullPath);
                string backupContent = File.ReadAllText(backupPath);

                // If the files are different, restore from backup
                if (currentContent != backupContent)
                {
                    File.Copy(backupPath, fullPath, true);
                    sb.AppendLine($"\n[Reapply] Restored: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                    sb.AppendLine($"Backup file: {Path.GetRelativePath(workspaceRoot, backupPath)}");

                    // Show a preview of the changes
                    sb.AppendLine("\n--- Changes ---");
                    sb.AppendLine($"Current lines: {currentContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                    sb.AppendLine($"Backup lines: {backupContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                    sb.AppendLine("--- End of Changes ---");
                }
                else
                {
                    sb.AppendLine($"[Reapply] No changes to restore for: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error during reapply: {ex.Message}");
                throw;
            }

            return sb.ToString();
        }

    }
}
