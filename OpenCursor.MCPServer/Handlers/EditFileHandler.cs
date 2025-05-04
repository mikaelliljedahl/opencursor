using OpenCursor.Client.Commands;
using System;
using System.IO;
using System.Text;

namespace OpenCursor.Client.Handlers
{
    public class EditFileHandler : IMcpCommandHandler
    {
        public string CommandName => "edit_file";

        public bool CanHandle(IMcpCommand command) => command is EditFileCommand;

        public async Task<string> HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not EditFileCommand editCmd)
            {
                return $"Expected EditFileCommand, got {command.GetType().Name}";
            }

            if (string.IsNullOrWhiteSpace(editCmd.TargetFile))
            {
                return "[File Edit] Missing target file path.";
                
            }

            if (string.IsNullOrWhiteSpace(editCmd.CodeEdit))
            {
                return "[File Edit] Empty code edit provided.";
                
            }
          
            StringBuilder sb = new StringBuilder();
            // Create backup of the original file
            string fullPath = IMcpCommandHandler.GetFullPath(editCmd.TargetFile, workspaceRoot);

            try
            {
                

                string backupPath = fullPath + ".bak"; // maybe we should put it in another folder, but for now we keep them in the same, requires .gitignore to include .bak
                File.Copy(fullPath, backupPath, true);

                // Read the original file
                string originalContent = File.ReadAllText(fullPath);
                string[] originalLines = originalContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Apply the edit (for now, we'll just replace the entire content)
                // In the future, we could implement more sophisticated diff/patch logic
                File.WriteAllText(fullPath, editCmd.CodeEdit);

                // Log the changes
                sb.AppendLine($"\n[File Edit] Successfully edited: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                sb.AppendLine($"Backup created at: {Path.GetRelativePath(workspaceRoot, backupPath)}");
                sb.AppendLine($"Instructions: {editCmd.Instructions}");

                // Show a preview of the changes
                sb.AppendLine("\n--- Changes ---");
                sb.AppendLine($"Original lines: {originalLines.Length}");
                sb.AppendLine($"New lines: {editCmd.CodeEdit.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                sb.AppendLine("--- End of Changes ---");

                // TODO: Implement more sophisticated diff viewing
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error during file edit: {ex.Message}");
                // If there was an error, restore from backup if it exists
                string backupPath = fullPath + ".bak";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, fullPath, true);
                        sb.AppendLine($"Restored from backup: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                    }
                    catch
                    {
                        sb.AppendLine($"Failed to restore from backup: {ex.Message}");
                    }
                }
                throw;
            }

            return sb.ToString();
        }

    }
}
