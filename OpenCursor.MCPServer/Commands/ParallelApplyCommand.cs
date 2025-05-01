using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCursor.Client.Commands
{
    // Represents a region within a file to be edited in parallel
    public class EditRegion
    {
        [JsonPropertyName("relative_workspace_path")]
        public string RelativeWorkspacePath { get; set; } = string.Empty;

        [JsonPropertyName("start_line")]
        public int? StartLine { get; set; } // Nullable if not always required by agent

        [JsonPropertyName("end_line")]
        public int? EndLine { get; set; }   // Nullable if not always required by agent
    }

    // Represents the parallel_apply command
    public class ParallelApplyCommand : IMcpCommand
    {
        public string CommandName => "parallel_apply";

        [JsonPropertyName("edit_plan")]
        public string EditPlan { get; set; } = string.Empty;

        [JsonPropertyName("edit_regions")]
        public List<EditRegion> EditRegions { get; set; } = new List<EditRegion>();
    }
}
