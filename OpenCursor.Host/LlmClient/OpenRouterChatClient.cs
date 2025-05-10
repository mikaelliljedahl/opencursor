using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenCursor.Host.LlmClient
{
    public class OpenRouterChatClient : IChatClient
    {
        //private IChatClient _chatClient;
        private OpenAI.Chat.ChatClient _openAIClient;
        string model = "qwen/qwen3-235b-a22b";
        string othermodel = "qwen/qwen3-30b-a3b:free";
        string mistralmodel = "mistralai/mistral-small-3.1-24b-instruct:free";

        //private readonly HttpClient _httpClient;


        public OpenRouterChatClient(IConfiguration configuration) 
        {
            var apiKey = configuration.GetValue<string>("OpenRouterApiKey");
           

            _openAIClient = new OpenAI.Chat.ChatClient(model, credential: new ApiKeyCredential(apiKey), 
                options: new OpenAIClientOptions()
            {
                    Endpoint = new Uri("https://openrouter.ai/api/v1"),
                    
                    
                });

        }

        public void Dispose()
        {
        }


        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // Convert Microsoft.Extensions.AI.ChatMessage to OpenAI.Chat.ChatMessage, we need a switch statement to create the correct type of chatmessage, e.g. User/assistant/tool
            var openAIMessages = messages.Select<ChatMessage, OpenAI.Chat.ChatMessage>(m =>
{
                    // Assuming Microsoft.Extensions.AI.ChatMessage has properties: Role, Contents
                    // Map roles to OpenAI.Chat.ChatRole
                    switch (m.Role.ToString().ToLowerInvariant())
                    {
                        case "user":
                            return OpenAI.Chat.ChatMessage.CreateUserMessage(m.Text);
                        case "assistant":
                            return OpenAI.Chat.ChatMessage.CreateAssistantMessage(m.Text);
                        case "system":
                            return OpenAI.Chat.ChatMessage.CreateSystemMessage(m.Text);
                        case "function":
                            return OpenAI.Chat.ChatMessage.CreateSystemMessage(m.Text);
                        case "tool":
                            return OpenAI.Chat.ChatMessage.CreateToolMessage(m.Text);
                        default:
                            // Fallback to user if unknown
                            return OpenAI.Chat.ChatMessage.CreateUserMessage(m.Text);
                    }
                }).ToList();

            // Call OpenRouter's API through the OpenAI client
            var result = await _openAIClient.CompleteChatAsync(
                openAIMessages,
                new OpenAI.Chat.ChatCompletionOptions()
                {
                    Temperature = options?.Temperature ?? 0.7f
                },
                cancellationToken);


            var message = result.Value;

            var response = new ChatResponse();

            var responseText = message.Content.FirstOrDefault()?.Text ?? string.Empty;
            var toolCall = ToolCallExtractor.TryExtractToolCall(responseText);

            if (message.ToolCalls?.Count > 0)
            {
                response.Messages = message.ToolCalls.Select(tc =>
                    new ChatMessage
                    {
                        Role = ChatRole.Tool,
                        Contents = [new FunctionCallContent(message.Id, tc.FunctionName, new Dictionary<string, object>()  )]
                    }).ToList();
                
                response.FinishReason = ChatFinishReason.ToolCalls;
                return response;
            }
            else if (toolCall != null && !string.IsNullOrWhiteSpace(toolCall.tool))
            {
                response.Messages = [

                    new ChatMessage
                    {
                        Role = ChatRole.Tool,
                        Contents = [new FunctionCallContent(message.Id, toolCall.tool, toolCall.parameters)]
                    } ];

                response.FinishReason = ChatFinishReason.ToolCalls; 
                return response;
            }
            else
            {
                response.Messages = message.Content.Select(m =>
                    new ChatMessage
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent(m.Text)]
                    }).ToList();

                response.FinishReason = ChatFinishReason.Stop;
                return response;
            }

            //ChatRole role = ChatRole.Assistant;
            //if (result.Value.Role == OpenAI.Chat.ChatMessageRole.Function)
            //    role = ChatRole.Tool;

            //if (result.Value.Role == OpenAI.Chat.ChatMessageRole.User)
            //    role = ChatRole.User;


            //// Map the response to ChatResponse
            //return new ChatResponse()
            //{
            //    Messages = result.Value.Content.Select(m =>
            //        new ChatMessage()
            //        {
            //            Role = role,
            //            Contents = [new TextContent(m.Text)]
                        
            //        }).ToList(),
            //    FinishReason = result.Value.FinishReason == OpenAI.Chat.ChatFinishReason.ToolCalls ? ChatFinishReason.ToolCalls : ChatFinishReason.Stop // ignore other reasons
            //};
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {

            return _openAIClient.AsIChatClient().GetService(serviceType, serviceKey);
        }

       

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var chatClient = _openAIClient.AsIChatClient();

            // Forward the streaming response from the wrapped client
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                yield return update;
            }
        }
    }
}
