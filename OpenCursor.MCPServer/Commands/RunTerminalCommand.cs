using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    public class RunTerminalCommand : IMcpCommand
    {
        public string CommandName => "run_terminal_cmd";

        [JsonPropertyName("command")]
        public string CommandLine { get; set; } = string.Empty;

        [JsonPropertyName("is_background")]
        public bool IsBackground { get; set; } = false;

        // require_user_approval might be handled by the agent/UI, not the client executor directly
        // [JsonPropertyName("require_user_approval")]
        // public bool RequireUserApproval { get; set; } = true; 
    }
}
