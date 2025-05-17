using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenCursor.Host
{
    public class McpClientService(IClientTransport clientTransport, IChatClient chatClient, ILogger<McpClientService> logger)
    {
        private IMcpClient _mcpClient;
        private IList<McpClientTool> _tools;

        public IMcpClient McpClient => _mcpClient;
        public IList<McpClientTool> Tools => _tools;

        public async Task InitializeAsync()
        {
            if (_mcpClient == null)
            {
                try
                {
                    
                    _mcpClient = await McpClientFactory.CreateAsync(clientTransport, clientOptions: new()
                    {
                        
                        Capabilities = new ClientCapabilities() { Sampling = new() { SamplingHandler = chatClient.CreateSamplingHandler() } }
                    });
                    _tools = await _mcpClient.ListToolsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not initialize McpClient");
                    _tools = new List<McpClientTool>();
                }
            }
        }
    }
}