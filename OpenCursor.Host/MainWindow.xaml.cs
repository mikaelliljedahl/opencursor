using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using ModelContextProtocol.Protocol.Types;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI;

namespace OpenCursor.Host
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        // --- State --- 
        private readonly ObservableCollection<string> _chatHistory; // For display
        private IChatClient _chatClient; // For LLM calls
        private IList<McpClientTool> _tools;
        List<Microsoft.Extensions.AI.ChatMessage> messages = []; // chathistory for the LLM

        public MainWindow(IServiceProvider serviceProvider) // IChatClient chatClient
        {   
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _chatHistory = new ObservableCollection<string>();
            ChatHistoryDisplay.ItemsSource = _chatHistory;

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Optionally load and display the system prompt initially
            // AddChatMessage($"System Prompt: {LoadSystemPrompt().Substring(0, 100)}...");

            var clientTransport = _serviceProvider.GetRequiredService<IClientTransport>();

            // Create a sampling client.
            var samplingClient = _serviceProvider.GetRequiredService<IChatClient>();


            var mcpClient = await McpClientFactory.CreateAsync(clientTransport, clientOptions: new()
            {
                Capabilities = new ClientCapabilities() { Sampling = new() { SamplingHandler = samplingClient.CreateSamplingHandler() } },
            });

            _tools = await mcpClient.ListToolsAsync();
            AddChatMessage($"Tools available:");

            foreach (var tool in _tools)
            {
                AddChatMessage($" {tool}");
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {   
        }


        // --- Chat Logic --- 
        public void AddChatMessage(string message)
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
           
            // Add user message to UI and internal history (Gemini format)
            AddChatMessage($"User: {userInput}");
            UserInputTextBox.Clear();

            SendButton.IsEnabled = false; // Disable button during API call

            if (_chatClient == null)
            {
                // Program hans if we add it to contructor
                _chatClient = _serviceProvider.GetRequiredService<IChatClient>();
            }

            try
            {
                messages.Add(new (ChatRole.User, userInput));
                List<ChatResponseUpdate> updates = [];
                await foreach (var response in _chatClient.GetStreamingResponseAsync(messages,
                    new ChatOptions()
                    {
                        Tools = [.. _tools],
                        ToolMode = ChatToolMode.Auto,
                        AllowMultipleToolCalls = true,
                        Temperature = (float?)0.8,

                    }))
                {
                    updates.Add(response);
                    AddChatMessage(response.Text);
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
        


    }
}