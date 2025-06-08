using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenCursor.Host.LlmClient;

namespace OpenCursor.Host
{
    public class ChatClientSelectorService : IDisposable
    {
        private readonly SettingsService _settingsService;
        private readonly IConfiguration _configuration;
        private IChatClient? _currentClient;
        private string? _lastClientType;

        public ChatClientSelectorService(SettingsService settingsService, IConfiguration configuration)
        {
            _settingsService = settingsService;
            _configuration = configuration;
        }

        public async Task<IChatClient> GetCurrentClientAsync()
        {
            var settings = await _settingsService.LoadAsync();
            if (_currentClient == null || _lastClientType != settings.ChatClient)
            {
                // Dispose previous client if needed
                if (_currentClient is IDisposable disposable)
                    disposable.Dispose();

                _lastClientType = settings.ChatClient;
                switch (settings.ChatClient)
                {
                    case "Gemini":
                        _currentClient = new WrappedGeminiChatClient(new ConfigurationBuilder()
                            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("GoogleApiKey", settings.GoogleApiKey) })
                            .Build());
                        break;
                    case "Claude":
                        _currentClient = new ClaudeChatClient(new ConfigurationBuilder()
                            .AddInMemoryCollection(new[] { 
                                new KeyValuePair<string, string?>("ClaudeApiKey", settings.ClaudeApiKey),
                                new KeyValuePair<string, string?>("ClaudeModel", settings.ClaudeModel)
                            })
                            .Build());
                        break;
                    default: // OpenRouter
                        _currentClient = new OpenRouterChatClient(new ConfigurationBuilder()
                            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("OpenRouterApiKey", settings.OpenRouterApiKey) })
                            .Build());
                        break;
                }
            }
            return _currentClient;
        }

        public void Dispose()
        {
            if (_currentClient is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
