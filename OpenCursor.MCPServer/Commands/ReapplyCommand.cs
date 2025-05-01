using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class ReapplyCommand : IMcpCommand
    {
        public string CommandName => "reapply";

        [JsonPropertyName("target_file")]
        public string TargetFile { get; set; } = string.Empty;

        // Map RelativePath for potential internal consistency/use
        [JsonIgnore] 
        public string RelativePath => TargetFile;
    }
}
