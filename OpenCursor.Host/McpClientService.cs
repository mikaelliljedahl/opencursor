using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OpenCursor.MCPServer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenCursor.Host
{
    public class McpClientService
    {
        
        private readonly IEnumerable<McpServerTool> _tools;

        public McpClientService(ILogger<McpClientService> logger, IEnumerable<McpServerTool> tools)
        {
            
            _tools = tools;
        }

        public IEnumerable<McpServerTool> GetAvailableTools()
        {
            return _tools;
        }
    }
}