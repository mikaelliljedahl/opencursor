using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using System.Text;
using System.IO;

namespace OpenCursor.Host.Pages
{
    public partial class Index : ComponentBase
    {
        [Inject]
        public McpClientService McpClientService { get; set; } = default!;
        [Inject]
        public ChatClientSelectorService ChatClientSelector { get; set; } = default!;

        private List<string> _chatHistory = new List<string>();
        private string _userInput = string.Empty;
        private bool _isSending = false;
        private List<ChatMessage> messages = new List<ChatMessage>();
        private List<string> _tools = new();

        protected override async Task OnInitializedAsync()
        {
            if (McpClientService != null)
            {
                _tools = McpClientService.GetAvailableTools().Select(t => t.ProtocolTool.Name).ToList();
            }

            AddChatMessage($"Tools available:");

            foreach (var tool in _tools)
            {
                AddChatMessage($" {tool}");
            }

            var systemPrompt = BuildSystemPrompt();
            messages.Add(new(ChatRole.System, systemPrompt));
        }

        public void AddChatMessage(string message)
        {
            _chatHistory.Add(message);
            StateHasChanged();
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_userInput) || _isSending)
            {
                return;
            }

            string userInput = _userInput.Trim();

            AddChatMessage($"User: {userInput}");
            _userInput = string.Empty;
            _isSending = true;
            StateHasChanged();

            try
            {
                messages.Add(new(ChatRole.User, userInput));
                // Use the selected chat client at runtime
                var chatClient = await ChatClientSelector.GetCurrentClientAsync();

                var chatOptions = new ChatOptions()
                {
                    Tools = McpClientService.GetAvailableToolsAsAITool() 
                };

                var response = await chatClient.GetResponseAsync(messages, chatOptions);
                if (!string.IsNullOrWhiteSpace(response.Text))
                {
                    AddChatMessage($"AI: {response.Text}");
                }

                if (response.FinishReason == ChatFinishReason.ToolCalls)
                {
                    AddChatMessage($"Tool call: {string.Join(" ", response.Messages.Select(m=>string.Join(" ",  m.Contents.Select(c=>c.ToString()).ToString())))}");
                }
            }
            catch (Exception ex)
            {
                AddChatMessage($"SYSTEM: Error during API call: {ex.Message}");
            }
            finally
            {
                _isSending = false;
                StateHasChanged();
            }
        }

        private async Task HandleInput(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await SendMessage();
            }
        }

        private string SystemPromptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemPrompt", "systemprompt.md");

        private string BuildSystemPrompt()
        {
            try
            {
                if (!File.Exists(SystemPromptFilePath))
                {
                    throw new FileNotFoundException("System prompt file not found.", SystemPromptFilePath);
                }

                var sb = new StringBuilder();
                var systemprompt = File.ReadAllText(SystemPromptFilePath).Trim();
                sb.Append(systemprompt);
                sb.AppendLine("Here are the functions/tools available in JSONSchema format, please call them using the role: \"tool\" and not \"assistant\" and always use json:");
                foreach (var tool in _tools)
                {
                    sb.AppendLine($"{tool}");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                string errorMsg = $"SYSTEM: WARNING! Could not load system prompt from {SystemPromptFilePath}. Error: {ex.Message}";
                Console.WriteLine(errorMsg);
                AddChatMessage(errorMsg);
                return "You are a helpful assistant. Format commands using json tags.";
            }
        }
    }
}
