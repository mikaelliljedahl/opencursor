using Microsoft.Extensions.AI; // Used for the return type Microsoft.Extensions.AI.AITool
using Microsoft.Extensions.Logging; // Added for ILogger
using ModelContextProtocol.Server; // Assuming McpServerTool is from here
using OpenAI.Responses;
using System.Collections.Generic;
using System.Linq;
// If Microsoft.Extensions.AI.Functions is the correct namespace,
// you might consider adding: using Microsoft.Extensions.AI.Functions;
// However, fully qualifying the call is safer to avoid ambiguity.

namespace OpenCursor.Host
{
    public class McpClientService
    {
        private readonly IEnumerable<McpServerTool> _tools;
        private readonly ILogger<McpClientService> _logger; // Store the logger

        public McpClientService(ILogger<McpClientService> logger, IEnumerable<McpServerTool> tools)
        {
            _logger = logger; // Assign the logger
            _tools = tools;
        }

        public IEnumerable<McpServerTool> GetAvailableTools()
        {
            return _tools;
        }

        // Converts the available MCP tools to Microsoft.Extensions.AI.AITool
        public IList<AITool> GetAvailableToolsAsAITool()
        {
            var aiTools = new List<AITool>();

            foreach (var tool in _tools)
            {
                var aiTool = AIFunctionFactory.Create(tool.InvokeAsync, name: tool.ProtocolTool.Name, description: tool.ProtocolTool.Description);
                //aiTool.JsonSchema = tool.ProtocolTool.InputSchema;
                aiTools.Add(aiTool);
            }

            return aiTools;
        }
    }
}
