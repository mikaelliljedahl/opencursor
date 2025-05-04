using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class DeleteFileCommand : IMcpCommand
    {
        public string CommandName => "delete_file";

        [JsonPropertyName("target_file")]
        public string TargetFile { get; set; } = string.Empty; 

        // Map RelativePath to TargetFile for compatibility if needed internally,
        // but TargetFile is the primary property based on the prompt.
        [JsonIgnore] // Don't serialize/deserialize RelativePath directly if using TargetFile
        public string RelativePath => TargetFile; 
    }
}