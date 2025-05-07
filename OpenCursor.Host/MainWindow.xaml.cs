using Microsoft.Extensions.AI;
using System.Collections.ObjectModel;
using System.Windows;

namespace OpenCursor.Host
{
    public partial class MainWindow : Window
    {
        // --- State --- 
        private readonly ObservableCollection<string> _chatHistory; // For display
        private readonly IChatClient _chatClient; // For LLM calls

        public MainWindow(IChatClient chatClient) 
        {   
            InitializeComponent();
            _chatHistory = new ObservableCollection<string>();
            _chatClient = chatClient;
            ChatHistoryDisplay.ItemsSource = _chatHistory;

           
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            // Optionally load and display the system prompt initially
            // AddChatMessage($"System Prompt: {LoadSystemPrompt().Substring(0, 100)}...");
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

            try
            {
                var response = await _chatClient.GetResponseAsync(userInput);
                AddChatMessage(response.Text);
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