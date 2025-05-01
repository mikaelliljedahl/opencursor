using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class FileSearchCommand : IMcpCommand
    {
        public string CommandName => "file_search";

        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;
    }
}
