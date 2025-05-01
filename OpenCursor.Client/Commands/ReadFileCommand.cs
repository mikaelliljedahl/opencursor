using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class ReadFileCommand : IMcpCommand
    {
        public string CommandName => "read_file"; // Matches the JSON name

        [JsonPropertyName("relative_workspace_path")]
        public string RelativePath { get; set; } = string.Empty;

        [JsonPropertyName("should_read_entire_file")]
        public bool ShouldReadEntireFile { get; set; } = false; // Default to false if not present

        [JsonPropertyName("start_line_one_indexed")]
        public int? StartLine { get; set; } // Nullable if not always present

        [JsonPropertyName("end_line_one_indexed_inclusive")]
        public int? EndLine { get; set; } // Nullable if not always present

        // We don't have 'Content' for a read command
    }
}
