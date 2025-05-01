using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace OpenCursor.Client.Commands
{
    public class CodebaseSearchCommand : IMcpCommand
    {
        public string CommandName => "codebase_search";

        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("target_directories")]
        public List<string>? TargetDirectories { get; set; }

        // Explanation is often part of the request but not needed for execution logic
        // [JsonPropertyName("explanation")] 
        // public string Explanation { get; set; } 
    }
}
