using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCursor.Host.LlmClient
{
    public class OpenRouterChatClient : IChatClient
    {
        private IChatClient _chatClient;
        //private readonly HttpClient _httpClient;


        public OpenRouterChatClient(IConfiguration configuration) 
        {
            var apiKey = configuration.GetValue<string>("OpenRouterApiKey");
            // Configure HttpClient with OpenRouter headers
            //_httpClient = new HttpClient();
            //_httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            

            //// Create OpenAIClient with custom HttpClient
            //var openAIClient1 = new OpenAIClient(new ApiKeyCredential(apiKey),
            //    new OpenAIClientOptions
            //    {
            //        Endpoint = new Uri("https://openrouter.ai/api/v1"),

            //        Transport = new 
            //    },
            //    new Uri("https://openrouter.ai/api/v1"),
            //    _httpClient
            //);


            var openAIClient = new OpenAI.Chat.ChatClient("qwen/qwen3-235b-a22b", credential: new ApiKeyCredential(apiKey), 
                options: new OpenAIClientOptions()
            {
                    Endpoint = new Uri("https://openrouter.ai/api/v1"),
                    
                    
                });


            _chatClient = openAIClient.AsIChatClient();   
        }

        public void Dispose()
        {
        }

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            return await _chatClient.GetResponseAsync(messages, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return _chatClient.GetService(serviceType, serviceKey);
        }

       

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Forward the streaming response from the wrapped client
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                yield return update;
            }
        }
    }
}
