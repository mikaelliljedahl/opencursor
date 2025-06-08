using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace OpenCursor.Host.LlmClient
{
    /// <summary>
    /// Claude chat client that integrates with Anthropic's Claude API.
    /// Note: This uses the official API rather than Pro subscription browser automation
    /// for security, reliability, and compliance reasons.
    /// </summary>
    public class ClaudeChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private const string AnthropicApiBaseUrl = "https://api.anthropic.com/v1/messages";

        public ClaudeChatClient(IConfiguration configuration, HttpClient? httpClient = null)
        {
            _apiKey = configuration.GetValue<string>("ClaudeApiKey") ?? throw new ArgumentException("ClaudeApiKey not configured");
            _model = configuration.GetValue<string>("ClaudeModel") ?? "claude-3-5-sonnet-20241022";
            
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var claudeMessages = ConvertToClaudeMessages(messages);
            var systemMessage = ExtractSystemMessage(messages);

            var request = new
            {
                model = _model,
                max_tokens = options?.MaxOutputTokens ?? 4000,
                temperature = options?.Temperature ?? 0.7,
                system = systemMessage,
                messages = claudeMessages,
                tools = ConvertTools(options?.Tools)
            };

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiBaseUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseJson);

            return ConvertToChatResponse(claudeResponse);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var claudeMessages = ConvertToClaudeMessages(messages);
            var systemMessage = ExtractSystemMessage(messages);

            var request = new
            {
                model = _model,
                max_tokens = options?.MaxOutputTokens ?? 4000,
                temperature = options?.Temperature ?? 0.7,
                system = systemMessage,
                messages = claudeMessages,
                stream = true,
                tools = ConvertTools(options?.Tools)
            };

            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiBaseUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var textBuilder = new StringBuilder();

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;

                var jsonData = line.Substring(6); // Remove "data: " prefix
                if (jsonData == "[DONE]") break;

                ClaudeStreamChunk? streamChunk = null;
                try
                {
                    streamChunk = JsonSerializer.Deserialize<ClaudeStreamChunk>(jsonData);
                }
                catch (JsonException)
                {
                    // Skip malformed JSON chunks
                    continue;
                }

                if (streamChunk?.delta?.text != null)
                {
                    textBuilder.Append(streamChunk.delta.text);
                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent(streamChunk.delta.text)]
                    };
                }
            }
        }

        private static List<object> ConvertToClaudeMessages(IEnumerable<ChatMessage> messages)
        {
            return messages
                .Where(m => m.Role != ChatRole.System) // System messages are handled separately
                .Select(m => new
                {
                    role = m.Role.ToString().ToLowerInvariant() switch
                    {
                        "assistant" => "assistant",
                        "user" => "user",
                        "tool" => "user", // Claude doesn't have a separate tool role
                        _ => "user"
                    },
                    content = GetContentText(m)
                })
                .Cast<object>()
                .ToList();
        }

        private static string? ExtractSystemMessage(IEnumerable<ChatMessage> messages)
        {
            var systemMessage = messages.FirstOrDefault(m => m.Role == ChatRole.System);
            return systemMessage != null ? GetContentText(systemMessage) : null;
        }

        private static string GetContentText(ChatMessage message)
        {
            if (!string.IsNullOrEmpty(message.Text))
                return message.Text;

            return string.Join("", message.Contents?.Select(c => c.ToString()) ?? []);
        }

        private static object[]? ConvertTools(IList<AITool>? tools)
        {
            if (tools == null || tools.Count == 0) return null;

            // For now, return a simplified tool conversion
            // This can be enhanced later when we understand the exact AITool structure
            return tools.Select((tool, index) => new
            {
                name = $"tool_{index}",
                description = "MCP Tool",
                input_schema = new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                }
            }).ToArray();
        }

        private static ChatResponse ConvertToChatResponse(ClaudeApiResponse? claudeResponse)
        {
            if (claudeResponse?.content == null || claudeResponse.content.Length == 0)
            {
                return new ChatResponse([]);
            }

            var messages = new List<ChatMessage>();
            var finishReason = ChatFinishReason.Stop;

            foreach (var content in claudeResponse.content)
            {
                if (content.type == "text" && !string.IsNullOrEmpty(content.text))
                {
                    messages.Add(new ChatMessage
                    {
                        Role = ChatRole.Assistant,
                        Contents = [new TextContent(content.text)]
                    });
                }
                else if (content.type == "tool_use")
                {
                    messages.Add(new ChatMessage
                    {
                        Role = ChatRole.Tool,
                        Contents = [new FunctionCallContent(
                            content.id ?? "",
                            content.name ?? "",
                            content.input as Dictionary<string, object> ?? new Dictionary<string, object>()
                        )]
                    });
                    finishReason = ChatFinishReason.ToolCalls;
                }
            }

            return new ChatResponse(messages) { FinishReason = finishReason };
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return serviceKey is not null ? null :
                serviceType == typeof(ClaudeChatClient) ? this :
                serviceType?.IsInstanceOfType(this) is true ? this :
                null;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        // Claude API response models
        private class ClaudeApiResponse
        {
            public string? id { get; set; }
            public string? type { get; set; }
            public string? role { get; set; }
            public ClaudeContent[]? content { get; set; }
            public string? model { get; set; }
            public string? stop_reason { get; set; }
            public object? usage { get; set; }
        }

        private class ClaudeContent
        {
            public string? type { get; set; }
            public string? text { get; set; }
            public string? id { get; set; }
            public string? name { get; set; }
            public object? input { get; set; }
        }

        private class ClaudeStreamChunk
        {
            public string? type { get; set; }
            public ClaudeStreamDelta? delta { get; set; }
        }

        private class ClaudeStreamDelta
        {
            public string? type { get; set; }
            public string? text { get; set; }
        }
    }
}