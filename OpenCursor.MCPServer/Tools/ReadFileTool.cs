using OpenCursor.Client.Handlers;
using System.Text;

namespace OpenCursor.MCPServer.Tools
{

    [McpServerToolType]
    public class ReadFileTool 
    {
        [McpServerTool, Description("Read the contents of a file. the output of this tool call will be the 1-indexed file contents from start_line_one_indexed to end_line_one_indexed_inclusive, together with a summary of the lines outside start_line_one_indexed and end_line_one_indexed_inclusive.\\\\nNote that this call can view at most 250 lines at a time.\\\\n\\\\nWhen using this tool to gather information, it's your responsibility to ensure you have the COMPLETE context. Specifically, each time you call this command you should:\\\\n1) Assess if the contents you viewed are sufficient to proceed with your task.\\\\n2) Take note of where there are lines not shown.\\\\n3) If the file contents you have viewed are insufficient, and you suspect they may be in lines not shown, proactively call the tool again to view those lines.\\\\n4) When in doubt, call this tool again to gather more information. Remember that partial file views may miss critical dependencies, imports, or functionality.\\\\n\\\\nIn some cases, if reading a range of lines is not enough, you may choose to read the entire file.\\\\nReading entire files is often wasteful and slow, especially for large files (i.e. more than a few hundred lines). So you should use this option sparingly.\\\\nReading the entire file is not allowed in most cases. You are only allowed to read the entire file if it has been edited or manually attached to the conversation by the user.\""),
            ]
        
        public static async Task<string> ReadFile(
            [Description("The path of the file to read, relative to the workspace root")] string relativePath,
            [Description("Whether to read the entire file. Defaults to false")] bool shouldReadEntireFile,
            [Description("The one-indexed line number to start reading from (inclusive).")] int? startLine, 
            [Description("The one-indexed line number to end reading at (inclusive)")] int? endLine)
        {

            string fullPath = IMcpCommandHandler.GetFullPath(relativePath, MCPServer.WorkspaceRoot);
            if (!File.Exists(fullPath))
            {
                return $"[Read File] File not found: {relativePath}";
            }
            try
            {
                Console.WriteLine($"\n[Read File] Content of: {Path.GetFileName(relativePath)}");
                if (shouldReadEntireFile || startLine == null || endLine == null)
                {
                    return await File.ReadAllTextAsync(fullPath);
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    var lines = File.ReadLines(fullPath)
                        .Skip(startLine.Value - 1)
                        .Take(endLine.Value - startLine.Value + 1);
                    foreach (var line in lines)
                    {
                        sb.AppendLine(line);
                        
                    }
                    
                    return sb.ToString();
                }
                

            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
                
            }
        }

    }
}
