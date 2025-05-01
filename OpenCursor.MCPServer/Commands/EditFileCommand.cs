using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class EditFileCommand : IMcpCommand
    {
        public string CommandName => "edit_file";

        [JsonPropertyName("target_file")]
        public string TargetFile { get; set; } = string.Empty;

        // Map RelativePath for potential internal consistency/use
        [JsonIgnore] 
        public string RelativePath => TargetFile;

        [JsonPropertyName("instructions")]
        public string Instructions { get; set; } = string.Empty;

        [JsonPropertyName("code_edit")]
        public string CodeEdit { get; set; } = string.Empty;

        [JsonPropertyName("blocking")]
        public bool Blocking { get; set; } = false; // Default based on description
    }
}
