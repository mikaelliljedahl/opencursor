using OpenCursor.Client.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenCursor.Client.Handlers
{
    public class ParallelApplyHandler : IMcpCommandHandler
    {
        public string CommandName => "parallel_apply";

        public bool CanHandle(IMcpCommand command) => command is ParallelApplyCommand;

        public async Task HandleCommand(IMcpCommand command, string workspaceRoot)
        {
            if (command is not ParallelApplyCommand parallelCmd)
            {
                throw new ArgumentException($"Expected ParallelApplyCommand, got {command.GetType().Name}");
            }

            if (string.IsNullOrWhiteSpace(parallelCmd.EditPlan) || parallelCmd.EditRegions == null || !parallelCmd.EditRegions.Any())
            {
                Console.WriteLine("[Parallel Apply] Invalid edit plan or empty regions.");
                return;
            }

            try
            {
                Console.WriteLine($"\n[Parallel Apply] Applying edit plan: '{Shorten(parallelCmd.EditPlan, 100)}'");
                Console.WriteLine($"Total regions to edit: {parallelCmd.EditRegions.Count}");

                // Process each region
                foreach (var region in parallelCmd.EditRegions)
                {
                    string fullPath = IMcpCommandHandler.GetFullPath(region.RelativeWorkspacePath, workspaceRoot);
                    if (!File.Exists(fullPath))
                    {
                        Console.WriteLine($"[Parallel Apply] File not found: {region.RelativeWorkspacePath}");
                        continue;
                    }

                    try
                    {
                        // Create backup
                        string backupPath = fullPath + ".bak";
                        File.Copy(fullPath, backupPath, true);

                        // Read the file
                        string content = File.ReadAllText(fullPath);
                        string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        // Validate line numbers
                        if (region.StartLine.HasValue && region.EndLine.HasValue && 
                            (region.StartLine.Value < 1 || region.EndLine.Value > lines.Length || 
                            region.StartLine.Value > region.EndLine.Value))
                        {
                            Console.WriteLine($"[Parallel Apply] Invalid line range for {region.RelativeWorkspacePath}: {region.StartLine}-{region.EndLine}");
                            continue;
                        }

                        // For now, just show what we would edit
                        // In the future, we would implement actual parallel editing logic here
                        Console.WriteLine($"\nProcessing region: {region.RelativeWorkspacePath}");
                        Console.WriteLine($"Line range: {region.StartLine ?? 1}-{region.EndLine ?? lines.Length}");
                        Console.WriteLine($"--- Preview ---");
                        
                        // Show the lines that would be edited
                        int startLine = region.StartLine ?? 1;
                        int endLine = region.EndLine ?? lines.Length;
                        for (int i = startLine - 1; i < endLine; i++)
                        {
                            Console.WriteLine($"{i + 1}: {lines[i]}" + (lines[i].Length > 80 ? "..." : ""));
                        }
                        Console.WriteLine("--- End of Preview ---");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing region {region.RelativeWorkspacePath}: {ex.Message}");
                    }
                }

                Console.WriteLine($"\n[Parallel Apply] Completed processing {parallelCmd.EditRegions.Count} regions");
                Console.WriteLine("Note: Actual parallel editing is not yet implemented.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during parallel apply: {ex.Message}");
                throw;
            }
        }

        private string Shorten(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text ?? string.Empty;
            return text.Substring(0, maxLength) + "...";
        }

    }
}
