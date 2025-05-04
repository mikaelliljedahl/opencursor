using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class GrepSearchCommand : IMcpCommand
    {
        public string CommandName => "grep_search";

        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("case_sensitive")]
        public bool? CaseSensitive { get; set; } // Nullable boolean

        [JsonPropertyName("include_pattern")]
        public string? IncludePattern { get; set; }

        [JsonPropertyName("exclude_pattern")]
        public string? ExcludePattern { get; set; }
    }
}
