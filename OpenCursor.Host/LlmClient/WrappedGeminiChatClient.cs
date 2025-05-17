using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpenCursor.Host.LlmClient;

/// <summary>
/// This is a wrapper around the Gemini API client mostly copied from:
/// https://github.com/mscraftsman/generative-ai/blob/main/src/Mscc.GenerativeAI.Microsoft/GeminiChatClient.cs
/// </summary>

public class WrappedGeminiChatClient : IChatClient
{

    private readonly string _googleApiKey;
    private readonly MainWindow _mainWindow;
    private readonly string _geminiModelName = "gemini-2.0-flash"; //  gemini-pro";
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    //private List<GeminiContent> _conversationHistory = new List<GeminiContent>(); // For API context (using Gemini structure)
    /// <summary>
    /// Gets the Gemini model that is used to communicate with.
    /// </summary>
    private readonly Mscc.GenerativeAI.GenerativeModel _client;
    private readonly ChatClientMetadata _metadata;

    public WrappedGeminiChatClient(IConfiguration configuration, MainWindow mainWindow)
    {
        _googleApiKey = configuration.GetValue<string>("GoogleApiKey");
        _mainWindow = mainWindow;

        var genAi = new Mscc.GenerativeAI.GoogleAI(_googleApiKey);
        _client = genAi.GenerativeModel(_geminiModelName);

        if (string.IsNullOrEmpty(_googleApiKey) || _googleApiKey == "YOUR_GOOGLE_API_KEY")
        {
            _mainWindow.AddChatMessage("SYSTEM: WARNING! Google API Key not configured. Please set _googleApiKey securely.");
        }
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));

        var request = Mscc.GenerativeAI.Microsoft.MicrosoftAi.AbstractionMapper.ToGeminiGenerateContentRequest(messages, options);
        var requestOptions = Mscc.GenerativeAI.Microsoft.MicrosoftAi.AbstractionMapper.ToGeminiGenerateContentRequestOptions(options);
        var response = await _client.GenerateContent(request, requestOptions);
        return Mscc.GenerativeAI.Microsoft.MicrosoftAi.AbstractionMapper.ToChatResponse(response) ?? new ChatResponse([]);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (messages == null) throw new ArgumentNullException(nameof(messages));

        var request = Mscc.GenerativeAI.Microsoft.MicrosoftAi.AbstractionMapper.ToGeminiGenerateContentRequest(messages, options);
        var requestOptions = Mscc.GenerativeAI.Microsoft.MicrosoftAi.AbstractionMapper.ToGeminiGenerateContentRequestOptions(options);
        await foreach (var response in _client.GenerateContentStream(request, requestOptions, cancellationToken))
            yield return Mscc.GenerativeAI.Microsoft.MicrosoftAi.AbstractionMapper.ToChatResponseUpdate(response);
    }

    //public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    //{
    //    var httpClient = HttpClientFactory.Create();
    //    string endpoint = $"{GeminiApiBaseUrl}{_geminiModelName}:generateContent?key={_googleApiKey}";

    //    var requestPayload = PrepareGeminiPayload(messages);
    //    // Use specific options to avoid serializing nulls if needed, or ensure classes are clean
    //    string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { IgnoreNullValues = true });

    //    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

    //    // No Authorization header needed, key is in URL
    //    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

    //    HttpResponseMessage response = await httpClient.SendAsync(request);

    //    string responseContent = await response.Content.ReadAsStringAsync(); // Read content regardless of status for debugging

    //    if (response.IsSuccessStatusCode)
    //    {
    //        // --- Parse the response --- 
    //        string llmResponseText = ParseGeminiResponse(responseContent);

    //        if (!string.IsNullOrEmpty(llmResponseText))
    //        {

    //            // Add AI response to conversation history for context in next turn
    //            _conversationHistory.Add(new GeminiContent { Role = "model", Parts = new List<GeminiPart> { new GeminiPart { Text = llmResponseText } } });

    //        }
    //        else
    //        {
    //            _mainWindow.AddChatMessage($"SYSTEM: Received empty or unparseable response from Gemini API. JSON: {responseContent.Substring(0, Math.Min(responseContent.Length, 200))}...");
    //        }
    //    }
    //    else
    //    {
    //        // Log the full error response for easier debugging
    //        _mainWindow.AddChatMessage($"SYSTEM: Gemini API Error {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 500))}...");
    //    }



    //var requestBody = new
    //{
    //    contents
    //};

    //var requestPayload = PrepareGeminiPayload();

    //var response = await httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);
    //response.EnsureSuccessStatusCode();

    //var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

    //var text = json
    //    .GetProperty("candidates")[0]
    //    .GetProperty("content")
    //    .GetProperty("parts")[0]
    //    .GetProperty("text")
    //    .GetString();

    //var assistantMessage = new ChatMessage(ChatRole.Assistant, responseContent);

    //    return new ChatResponse(assistantMessage);
    //}

    //public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    //{
    //    // Simulate streaming by splitting the final response into chunks
    //    var response = await GetResponseAsync(messages, options, cancellationToken);
    //    var content = string.Join(" ", response.Messages.SelectMany(m => m.Contents).Select(m => m.RawRepresentation.ToString()));

    //    var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //    var builder = new StringBuilder();

    //    foreach (var word in words)
    //    {
    //        builder.Append(word).Append(' ');


    //        // Add AI response to UI and internal history
    //        _mainWindow.AddChatMessage($"AI: {builder.ToString().TrimEnd()}");
    //        yield return new ChatResponseUpdate(
    //            ChatRole.Assistant, builder.ToString().TrimEnd());

    //    }
    //}


    /// <inheritdoc/>
    object? IChatClient.GetService(Type serviceType, object? serviceKey) =>
        serviceKey is not null ? null :
        serviceType == typeof(ChatClientMetadata) ? _metadata :
        serviceType?.IsInstanceOfType(this) is true ? this :
        null;

    public void Dispose()
    {

    }



    //// Helper to prepare the payload for Gemini API
    //private object PrepareGeminiPayload(IEnumerable<ChatMessage> messages)
    //{
    //    // Gemini uses a specific 'contents' structure
    //    // Combine system prompt (as initial user message or dedicated system instruction if supported)
    //    // and the conversation history.

    //    var combinedContents = new List<GeminiContent>();

    //    // 1. Add System Prompt (formatted as the first 'user' turn for context)
    //    // Note: Some models might support a dedicated 'systemInstruction' field later.
    //    // Check Gemini docs for the specific model if this doesn't work well.
    //    string systemPrompt = LoadSystemPrompt();
    //    if (!string.IsNullOrEmpty(systemPrompt))
    //    {
    //        combinedContents.Add(new GeminiContent
    //        {
    //            Role = "user",
    //            Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
    //        });
    //        // Add a placeholder model response if required by API for strict user/model alternation
    //        combinedContents.Add(new GeminiContent
    //        {
    //            Role = "model",
    //            Parts = new List<GeminiPart> { new GeminiPart { Text = "Okay, I understand the instructions." } } // Or just an empty part if allowed
    //        });
    //    }

    //    var geminiContents = messages
    //        .Where(m => m.Role != ChatRole.System) // Gemini doesn't support "system" role
    //        .Select(m => new GeminiContent
    //        {
    //            Role = m.Role.ToString().ToLower(), // Convert ChatRole to lowercase string for Gemini
    //            Parts = new List<GeminiPart> { new GeminiPart { Text = string.Join("", m.Contents.Select(c => c.ToString())) } }
    //        }).ToList();

    //    // 2. Add actual conversation history (from the argument)
    //    combinedContents.AddRange(geminiContents);

    //    // Construct the final payload object
    //    return new
    //    {
    //        contents = combinedContents,
    //        // generationConfig = new { ... } // Optional: Add temp, topP, maxTokens etc. here
    //    };
    //}

    //// Helper to parse the Gemini API response
    //private string ParseGeminiResponse(string jsonResponse)
    //{
    //    try
    //    {
    //        using (JsonDocument document = JsonDocument.Parse(jsonResponse))
    //        {
    //            // Navigate the typical Gemini structure: candidates -> content -> parts -> text
    //            if (document.RootElement.TryGetProperty("candidates", out JsonElement candidatesElement) && candidatesElement.ValueKind == JsonValueKind.Array && candidatesElement.GetArrayLength() > 0)
    //            {
    //                var firstCandidate = candidatesElement[0];
    //                if (firstCandidate.TryGetProperty("content", out JsonElement contentElement) && contentElement.TryGetProperty("parts", out JsonElement partsElement) && partsElement.ValueKind == JsonValueKind.Array && partsElement.GetArrayLength() > 0)
    //                {
    //                    var firstPart = partsElement[0];
    //                    if (firstPart.TryGetProperty("text", out JsonElement textElement))
    //                    {
    //                        return textElement.GetString()?.Trim();
    //                    }
    //                }
    //            }

    //            // Handle potential errors reported in the response body
    //            if (document.RootElement.TryGetProperty("error", out JsonElement errorElement))
    //            {
    //                _mainWindow.AddChatMessage($"SYSTEM: Gemini API returned an error: {errorElement.ToString()}");
    //                return null;
    //            }
    //            if (document.RootElement.TryGetProperty("promptFeedback", out JsonElement feedbackElement) &&
    //                feedbackElement.TryGetProperty("blockReason", out JsonElement blockReasonElement))
    //            {
    //                _mainWindow.AddChatMessage($"SYSTEM: Gemini API blocked the prompt. Reason: {blockReasonElement.GetString()}");
    //                return null;
    //            }

    //        }
    //        _mainWindow.AddChatMessage($"SYSTEM: Could not find expected text in Gemini API response: {jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 100))}...");
    //        return null; // Indicate parsing failure
    //    }
    //    catch (JsonException jsonEx)
    //    {
    //        _mainWindow.AddChatMessage($"SYSTEM: Failed to parse Gemini API JSON response: {jsonEx.Message}. Response: {jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 100))}...");
    //        return null;
    //    }
    //    catch (Exception ex)
    //    {
    //        _mainWindow.AddChatMessage($"SYSTEM: Unexpected error parsing Gemini API response: {ex.Message}");
    //        return null;
    //    }
    //}


    //// --- Gemini-specific data structures --- 
    //private class GeminiContent
    //{
    //    [JsonPropertyName("role")]
    //    public string Role { get; set; } // "user" or "model"
    //    [JsonPropertyName("parts")]
    //    public List<GeminiPart> Parts { get; set; }
    //}

    //private class GeminiPart
    //{
    //    [JsonPropertyName("text")]
    //    public string Text { get; set; }
    //    // Could add other part types like 'inlineData' if needed later
    //}

}
