using System;
using System.Collections.Generic; // Added for List<T>
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OpenCursor.BrowserHost
{
    public partial class MainWindow : Window
    {
        // --- Configuration --- 
        // TODO: Replace with your actual Google API Key (Load securely! e.g., User Secrets, Env Vars)
        private string _googleApiKey = ""; 
        private string _geminiModelName = "gemini-2.0-flash"; // Or other suitable model
        private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string ConsoleAppWebSocketUrl = "ws://localhost:12346/"; // Console App's server
        private string SystemPromptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemPrompt", "systemprompt.md"); // Assuming it's copied to output

        // --- State --- 
        private readonly HttpClient _httpClient;
        private ClientWebSocket _clientWebSocket;
        private readonly ObservableCollection<string> _chatHistory; // For display
        private List<GeminiContent> _conversationHistory; // For API context (using Gemini structure)

        public MainWindow()
        {   
            InitializeComponent();
            _chatHistory = new ObservableCollection<string>();
            _conversationHistory = new List<GeminiContent>();
            ChatHistoryDisplay.ItemsSource = _chatHistory;

            _httpClient = new HttpClient();
            // Configure HttpClient (e.g., timeout)

            // Placeholder: Load API key securely here
            if (string.IsNullOrEmpty(_googleApiKey) || _googleApiKey == "YOUR_GOOGLE_API_KEY")
            {
                AddChatMessage("SYSTEM: WARNING! Google API Key not configured. Please set _googleApiKey securely.");
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            await ConnectWebSocketAsync();
            // Optionally load and display the system prompt initially
            // AddChatMessage($"System Prompt: {LoadSystemPrompt().Substring(0, 100)}...");
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {   
            _httpClient?.Dispose();
            await DisconnectWebSocketAsync();
        }

        // --- WebSocket Client Logic (Connecting to Console App) ---
        private async Task ConnectWebSocketAsync()
        {
            if (_clientWebSocket != null && _clientWebSocket.State != WebSocketState.Closed && _clientWebSocket.State != WebSocketState.Aborted)
            {
                await DisconnectWebSocketAsync();
            }

            _clientWebSocket = new ClientWebSocket();
            try
            {
                AddChatMessage("System: Connecting to Console App...");
                await _clientWebSocket.ConnectAsync(new Uri(ConsoleAppWebSocketUrl), CancellationToken.None);
                AddChatMessage("System: Connected to Console App.");
                // Start listening for messages from console (if needed for bi-directional)
                // Task.Run(async () => await ListenForConsoleMessages()); 
            }
            catch (Exception ex)
            {
                AddChatMessage($"System: WebSocket connection failed: {ex.Message}");
                _clientWebSocket?.Dispose();
                _clientWebSocket = null;
            }
        }

        private async Task DisconnectWebSocketAsync()
        {
            try
            {   
                if (_clientWebSocket?.State == WebSocketState.Open)
                {
                    await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing application", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket closing error: {ex.Message}"); // Log only
            }
            finally
            {
                _clientWebSocket?.Dispose();
                _clientWebSocket = null;
                // Use Dispatcher if needed to update UI about disconnection
                // Dispatcher.Invoke(() => AddChatMessage("System: Disconnected from Console App."));
                Console.WriteLine("WebSocket client disposed.");
            }
        }

        private async Task SendResponseToConsoleAppAsync(string rawLlmResponse)
        {
            if (_clientWebSocket == null || _clientWebSocket.State != WebSocketState.Open)
            {
                AddChatMessage("System: Cannot send response. Not connected to Console App.");
                 return; 
            }

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(rawLlmResponse);
                await _clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Sent LLM response to Console App.");
            }
            catch (Exception ex)
            {   
                AddChatMessage($"System: Error sending response to Console App: {ex.Message}");
                await DisconnectWebSocketAsync(); // Disconnect on send error
            }
        }

        // --- Chat Logic --- 
        private void AddChatMessage(string message)
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.Invoke(() => 
            {            
                _chatHistory.Add(message); 
                ChatScrollViewer.ScrollToBottom();
            });
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {   
            string userInput = UserInputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(_googleApiKey) || _googleApiKey == "YOUR_GOOGLE_API_KEY")
            {
                if (string.IsNullOrEmpty(userInput)) return;
                AddChatMessage("SYSTEM: Cannot send message. Google API Key not configured.");
                return;
            }

            // Add user message to UI and internal history (Gemini format)
            AddChatMessage($"User: {userInput}");
            _conversationHistory.Add(new GeminiContent { Role = "user", Parts = new List<GeminiPart> { new GeminiPart { Text = userInput } } });
            UserInputTextBox.Clear();

            SendButton.IsEnabled = false; // Disable button during API call

            try
            {   
                // Prepare request for Google Gemini API
                string endpoint = $"{GeminiApiBaseUrl}{_geminiModelName}:generateContent?key={_googleApiKey}";
                var requestPayload = PrepareGeminiPayload();
                // Use specific options to avoid serializing nulls if needed, or ensure classes are clean
                string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { IgnoreNullValues = true }); 

                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    // No Authorization header needed, key is in URL
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    string responseContent = await response.Content.ReadAsStringAsync(); // Read content regardless of status for debugging

                    if (response.IsSuccessStatusCode)
                    {   
                        // --- Parse the response --- 
                        string llmResponseText = ParseGeminiResponse(responseContent);
                        
                        if (!string.IsNullOrEmpty(llmResponseText))
                        {
                             // Add AI response to UI and internal history
                            AddChatMessage($"AI: {llmResponseText}");
                            // Add AI response to conversation history for context in next turn
                            _conversationHistory.Add(new GeminiContent { Role = "model", Parts = new List<GeminiPart> { new GeminiPart { Text = llmResponseText } } }); 

                            // Send raw response to Console App via WebSocket
                            await SendResponseToConsoleAppAsync(llmResponseText); 
                        }
                        else
                        {
                            AddChatMessage($"SYSTEM: Received empty or unparseable response from Gemini API. JSON: {responseContent.Substring(0, Math.Min(responseContent.Length, 200))}...");
                        }
                    }
                    else
                    {
                        // Log the full error response for easier debugging
                        AddChatMessage($"SYSTEM: Gemini API Error {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {responseContent.Substring(0, Math.Min(responseContent.Length, 500))}..."); 
                    }
                }
            }
            catch (Exception ex)
            {
                AddChatMessage($"SYSTEM: Error during API call: {ex.Message}");
            }
            finally
            {
                 Dispatcher.Invoke(() => SendButton.IsEnabled = true); // Re-enable button
            }
        }
        
        // Helper to prepare the payload for Gemini API
        private object PrepareGeminiPayload()
        {
            // Gemini uses a specific 'contents' structure
            // Combine system prompt (as initial user message or dedicated system instruction if supported)
            // and the conversation history.
            
            var combinedContents = new List<GeminiContent>();

            // 1. Add System Prompt (formatted as the first 'user' turn for context)
            // Note: Some models might support a dedicated 'systemInstruction' field later.
            // Check Gemini docs for the specific model if this doesn't work well.
            string systemPrompt = LoadSystemPrompt();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                combinedContents.Add(new GeminiContent 
                {
                    Role = "user", 
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } } 
                });
                // Add a placeholder model response if required by API for strict user/model alternation
                 combinedContents.Add(new GeminiContent 
                 { 
                     Role = "model", 
                     Parts = new List<GeminiPart> { new GeminiPart { Text = "Okay, I understand the instructions." } } // Or just an empty part if allowed
                 });
            }

            // 2. Add actual conversation history
            combinedContents.AddRange(_conversationHistory);

            // Construct the final payload object
            return new 
            {
                contents = combinedContents
                // generationConfig = new { ... } // Optional: Add temp, topP, maxTokens etc. here
            };
        }

        // Helper to parse the Gemini API response
        private string ParseGeminiResponse(string jsonResponse)
        {
            try
            {   
                using (JsonDocument document = JsonDocument.Parse(jsonResponse))
                {
                    // Navigate the typical Gemini structure: candidates -> content -> parts -> text
                    if (document.RootElement.TryGetProperty("candidates", out JsonElement candidatesElement) && candidatesElement.ValueKind == JsonValueKind.Array && candidatesElement.GetArrayLength() > 0)
                    {   
                         var firstCandidate = candidatesElement[0];
                        if (firstCandidate.TryGetProperty("content", out JsonElement contentElement) && contentElement.TryGetProperty("parts", out JsonElement partsElement) && partsElement.ValueKind == JsonValueKind.Array && partsElement.GetArrayLength() > 0)
                        {
                            var firstPart = partsElement[0];
                            if (firstPart.TryGetProperty("text", out JsonElement textElement))
                            {
                                return textElement.GetString()?.Trim();
                            }
                        }
                    }
                    
                    // Handle potential errors reported in the response body
                    if (document.RootElement.TryGetProperty("error", out JsonElement errorElement))
                    {
                         AddChatMessage($"SYSTEM: Gemini API returned an error: {errorElement.ToString()}");
                         return null;
                    }
                     if (document.RootElement.TryGetProperty("promptFeedback", out JsonElement feedbackElement) && 
                         feedbackElement.TryGetProperty("blockReason", out JsonElement blockReasonElement))
                    {
                         AddChatMessage($"SYSTEM: Gemini API blocked the prompt. Reason: {blockReasonElement.GetString()}");
                         return null;
                    }

                }
                AddChatMessage($"SYSTEM: Could not find expected text in Gemini API response: {jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 100))}...");
                return null; // Indicate parsing failure
            }
            catch (JsonException jsonEx)
            {
                 AddChatMessage($"SYSTEM: Failed to parse Gemini API JSON response: {jsonEx.Message}. Response: {jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 100))}...");
                 return null;
            }
             catch (Exception ex)
            {
                 AddChatMessage($"SYSTEM: Unexpected error parsing Gemini API response: {ex.Message}");
                 return null;
            }
        }

        private string LoadSystemPrompt() // Renamed from BuildSystemPrompt for clarity
        {
            try
            {   
                if (!File.Exists(SystemPromptFilePath))
                {
                    throw new FileNotFoundException("System prompt file not found.", SystemPromptFilePath);
                }
                return File.ReadAllText(SystemPromptFilePath).Trim(); 
            }
            catch (Exception ex)
            {   
                string errorMsg = $"SYSTEM: WARNING! Could not load system prompt from {SystemPromptFilePath}. Error: {ex.Message}";
                Console.WriteLine(errorMsg);
                AddChatMessage(errorMsg);
                // Return a default minimal prompt
                return "You are a helpful assistant. Format commands using XML tags like <create_file path='...'>content</create_file>."; 
            }
        }

        // --- Gemini-specific data structures --- 
        private class GeminiContent
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } // "user" or "model"
            [JsonPropertyName("parts")]
            public List<GeminiPart> Parts { get; set; }
        }

        private class GeminiPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
            // Could add other part types like 'inlineData' if needed later
        }

        // Removed ChatMessage class (replaced by GeminiContent/Part used in _conversationHistory)
        // Removed Hugging Face specific methods/classes
    }
}