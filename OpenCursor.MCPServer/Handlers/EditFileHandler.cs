using OpenCursor.Client.Commands;
using System;
using System.IO;

namespace OpenCursor.Client.Handlers
{
    public class EditFileHandler : IMcpCommandHandler
    {
        public string CommandName => "edit_file";

        public bool CanHandle(IMcpCommand command) => command is EditFileCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not EditFileCommand editCmd)
            {
                throw new ArgumentException($"Expected EditFileCommand, got {command.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace(editCmd.TargetFile))
            {
                Console.WriteLine("[File Edit] Missing target file path.");
                return;
            }

            if (string.IsNullOrWhiteSpace(editCmd.CodeEdit))
            {
                Console.WriteLine("[File Edit] Empty code edit provided.");
                return;
            }
            // Create backup of the original file
            string fullPath = IMcpCommandHandler.GetFullPath(editCmd.TargetFile, workspaceRoot );

            try
            {
               
                string backupPath = fullPath + ".bak";
                File.Copy(fullPath, backupPath, true);

                // Read the original file
                string originalContent = File.ReadAllText(fullPath);
                string[] originalLines = originalContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Apply the edit (for now, we'll just replace the entire content)
                // In the future, we could implement more sophisticated diff/patch logic
                File.WriteAllText(fullPath, editCmd.CodeEdit);

                // Log the changes
                Console.WriteLine($"\n[File Edit] Successfully edited: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                Console.WriteLine($"Backup created at: {Path.GetRelativePath(workspaceRoot, backupPath)}");
                Console.WriteLine($"Instructions: {editCmd.Instructions}");

                // Show a preview of the changes
                Console.WriteLine("\n--- Changes ---");
                Console.WriteLine($"Original lines: {originalLines.Length}");
                Console.WriteLine($"New lines: {editCmd.CodeEdit.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length}");
                Console.WriteLine("--- End of Changes ---");

                // TODO: Implement more sophisticated diff viewing
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file edit: {ex.Message}");
                // If there was an error, restore from backup if it exists
                string backupPath = fullPath + ".bak";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, fullPath, true);
                        Console.WriteLine($"Restored from backup: {Path.GetRelativePath(workspaceRoot, fullPath)}");
                    }
                    catch
                    {
                        Console.WriteLine($"Failed to restore from backup: {ex.Message}");
                    }
                }
                throw;
            }
        }

    }
}
