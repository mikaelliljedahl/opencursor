using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class ListDirCommand : IMcpCommand
    {
        public string CommandName => "list_dir";

        [JsonPropertyName("relative_workspace_path")]
        public string RelativePath { get; set; } = string.Empty;
    }
}
