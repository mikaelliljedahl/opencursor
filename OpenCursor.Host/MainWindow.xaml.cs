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
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;

namespace OpenCursor.Host;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    // --- State --- 
    private readonly ObservableCollection<string> _chatHistory; // For display
    private IChatClient _chatClient; // For LLM calls
    List<Microsoft.Extensions.AI.ChatMessage> messages = []; // chathistory for the LLM, we must always send the complete history incl system prompt

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

        _clientTransport = _serviceProvider.GetRequiredService<IClientTransport>();

        // Create a sampling client.
        _samplingClient = _serviceProvider.GetRequiredService<IChatClient>();


        var mcpClient = await McpClientFactory.CreateAsync(_clientTransport, clientOptions: new()
        {
            Capabilities = new ClientCapabilities() { Sampling = new() { SamplingHandler = _samplingClient.CreateSamplingHandler() } },
        });


        _tools = await mcpClient.ListToolsAsync();
        AddChatMessage($"Tools available:");

        foreach (var tool in _tools)
        {
            AddChatMessage($" {tool}");
        }

        var systemPrompt = BuildSystemPrompt();
        messages.Add(new(ChatRole.System, systemPrompt));
        
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
                    ResponseFormat = ChatResponseFormat.Json,
                    

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

    private string SystemPromptFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemPrompt", "systemprompt.md"); // Assuming it's copied to output
    private IClientTransport _clientTransport;
    private IChatClient _samplingClient;
    private IList<McpClientTool> _tools;

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

            // now add tools
            // Here are the functions/tools available in JSONSchema format:

            foreach (var tool in _tools)
            {
                sb.AppendLine($"{tool.JsonSchema}");
            }

            //            Here are the functions available in JSONSchema format:
            //\< functions >
            //\< function >{ "description": "Find snippets of code from the codebase most relevant to the search query.\\nThis is a semantic search tool, so the query should ask for something semantically matching what is needed.\\nIf it makes sense to only search in particular directories, please specify them in the target_directories field.\\nUnless there is a clear reason to use your own search query, please just reuse the user's exact query with their wording.\\nTheir exact wording/phrasing can often be helpful for the semantic search query. Keeping the same exact question format can also be helpful.", "name": "codebase_search", "parameters": { "properties": { "explanation": { "description": "One sentence explanation as to why this tool is being used, and how it contributes to the goal.", "type": "string"}, "query": { "description": "The search query to find relevant code. You should reuse the user's exact query/most recent message with their wording unless there is a clear reason not to.", "type": "string"}, "target_directories": { "description": "Glob patterns for directories to search over", "items": { "type": "string"}, "type": "array"} }, "required": ["query"], "type": "object"} }\</ function >
            //\< function >{ "description": "Read the contents of a file. the output of this tool call will be the 1-indexed file contents from start_line_one_indexed to end_line_one_indexed_inclusive, together with a summary of the lines outside start_line_one_indexed and end_line_one_indexed_inclusive.\\nNote that this call can view at most 250 lines at a time.\\n\\nWhen using this tool to gather information, it's your responsibility to ensure you have the COMPLETE context. Specifically, each time you call this command you should:\\n1) Assess if the contents you viewed are sufficient to proceed with your task.\\n2) Take note of where there are lines not shown.\\n3) If the file contents you have viewed are insufficient, and you suspect they may be in lines not shown, proactively call the tool again to view those lines.\\n4) When in doubt, call this tool again to gather more information. Remember that partial file views may miss critical dependencies, imports, or functionality.\\n\\nIn some cases, if reading a range of lines is not enough, you may choose to read the entire file.\\nReading entire files is often wasteful and slow, especially for large files (i.e. more than a few hundred lines). So you should use this option sparingly.\\nReading the entire file is not allowed in most cases. You are only allowed to read the entire file if it has been edited or manually attached to the conversation by the user.", "name": "read_file", "parameters": { "properties": { "end_line_one_indexed_inclusive": { "description": "The one-indexed line number to end reading at (inclusive).", "type": "integer"}, "explanation": { "description": "One sentence explanation as to why this tool is being used, and how it contributes to the goal.", "type": "string"}, "relative_workspace_path": { "description": "The path of the file to read, relative to the workspace root.", "type": "string"}, "should_read_entire_file": { "description": "Whether to read the entire file. Defaults to false.", "type": "boolean"}, "start_line_one_indexed": { "description": "The one-indexed line number to start reading from (inclusive).", "type": "integer"} }, "required": ["relative_workspace_path", "should_read_entire_file", "start_line_one_indexed", "end_line_one_indexed_inclusive"], "type": "object"} }\</ function >
            //\< function >{ "description": "PROPOSE a command to run on behalf of the user.\\nIf you have this tool, note that you DO have the ability to run commands directly on the USER's system.\\nNote that the user will have to approve the command before it is executed.\\nThe user may reject it if it is not to their liking, or may modify the command before approving it.  If they do change it, take those changes into account.\\nThe actual command will NOT execute until the user approves it. The user may not approve it immediately. Do NOT assume the command has started running.\\nIf the step is WAITING for user approval, it has NOT started running.\\nIn using these tools, adhere to the following guidelines:\\n1. Based on the contents of the conversation, you will be told if you are in the same shell as a previous step or a different shell.\\n2. If in a new shell, you should `cd` to the appropriate directory and do necessary setup in addition to running the command.\\n3. If in the same shell, the state will persist (eg. if you cd in one step, that cwd is persisted next time you invoke this tool).\\n4. For ANY commands that would use a pager or require user interaction, you should append ` | cat` to the command (or whatever is appropriate). Otherwise, the command will break. You MUST do this for: git, less, head, tail, more, etc.\\n5. For commands that are long running/expected to run indefinitely until interruption, please run them in the background. To run jobs in the background, set `is_background` to true rather than changing the details of the command.\\n6. Dont include any newlines in the command.", "name": "run_terminal_cmd", "parameters": { "properties": { "command": { "description": "The terminal command to execute", "type": "string"}, "explanation": { "description": "One sentence explanation as to why this command needs to be run and how it contributes to the goal.", "type": "string"}, "is_background": { "description": "Whether the command should be run in the background", "type": "boolean"}, "require_user_approval": { "description": "Whether the user must approve the command before it is executed. Only set this to true if the command is safe and if it matches the user's requirements for commands that should be executed automatically.", "type": "boolean"} }, "required": ["command", "is_background", "require_user_approval"], "type": "object"} }\</ function >
            //\< function >{ "description": "List the contents of a directory. The quick tool to use for discovery, before using more targeted tools like semantic search or file reading. Useful to try to understand the file structure before diving deeper into specific files. Can be used to explore the codebase.", "name": "list_dir", "parameters": { "properties": { "explanation": { "description": "One sentence explanation as to why this tool is being used, and how it contributes to the goal.", "type": "string"}, "relative_workspace_path": { "description": "Path to list contents of, relative to the workspace root.", "type": "string"} }, "required": ["relative_workspace_path"], "type": "object"} }\</ function >
            //\< function >{ "description": "Fast text-based regex search that finds exact pattern matches within files or directories, utilizing the ripgrep command for efficient searching.\\nResults will be formatted in the style of ripgrep and can be configured to include line numbers and content.\\nTo avoid overwhelming output, the results are capped at 50 matches.\\nUse the include or exclude patterns to filter the search scope by file type or specific paths.\\n\\nThis is best for finding exact text matches or regex patterns.\\nMore precise than semantic search for finding specific strings or patterns.\\nThis is preferred over semantic search when we know the exact symbol/function name/etc. to search in some set of directories/file types.", "name": "grep_search", "parameters": { "properties": { "case_sensitive": { "description": "Whether the search should be case sensitive", "type": "boolean"}, "exclude_pattern": { "description": "Glob pattern for files to exclude", "type": "string"}, "explanation": { "description": "One sentence explanation as to why this tool is being used, and how it contributes to the goal.", "type": "string"}, "include_pattern": { "description": "Glob pattern for files to include (e.g. '*.ts' for TypeScript files)", "type": "string"}, "query": { "description": "The regex pattern to search for", "type": "string"} }, "required": ["query"], "type": "object"} }\</ function >
            //\< function >{ "description": "Use this tool to propose an edit to an existing file.\\n\\nThis will be read by a less intelligent model, which will quickly apply the edit. You should make it clear what the edit is, while also minimizing the unchanged code you write.\\nWhen writing the edit, you should specify each edit in sequence, with the special comment `// ... existing code ...` to represent unchanged code in between edited lines.\\n\\nFor example:\\n\\n```\\n// ... existing code ...\\nFIRST_EDIT\\n// ... existing code ...\\nSECOND_EDIT\\n// ... existing code ...\\nTHIRD_EDIT\\n// ... existing code ...\\n```\\n\\nYou should still bias towards repeating as few lines of the original file as possible to convey the change.\\nBut, each edit should contain sufficient context of unchanged lines around the code you're editing to resolve ambiguity.\\nDO NOT omit spans of pre-existing code without using the `// ... existing code ...` comment to indicate its absence.\\nMake sure it is clear what the edit should be.\\n\\nYou should specify the following arguments before the others: [target_file]", "name": "edit_file", "parameters": { "properties": { "blocking": { "description": "Whether this tool call should block the client from making further edits to the file until this call is complete. If true, the client will not be able to make further edits to the file until this call is complete.", "type": "boolean"}, "code_edit": { "description": "Specify ONLY the precise lines of code that you wish to edit. **NEVER specify or write out unchanged code**. Instead, represent all unchanged code using the comment of the language you're editing in - example: `// ... existing code ...`", "type": "string"}, "instructions": { "description": "A single sentence instruction describing what you are going to do for the sketched edit. This is used to assist the less intelligent model in applying the edit. Please use the first person to describe what you are going to do. Dont repeat what you have said previously in normal messages. And use it to disambiguate uncertainty in the edit.", "type": "string"}, "target_file": { "description": "The target file to modify. Always specify the target file as the first argument and use the relative path in the workspace of the file to edit", "type": "string"} }, "required": ["target_file", "instructions", "code_edit", "blocking"], "type": "object"} }\</ function >
            //\< function >{ "description": "Fast file search based on fuzzy matching against file path. Use if you know part of the file path but don't know where it's located exactly. Response will be capped to 10 results. Make your query more specific if need to filter results further.", "name": "file_search", "parameters": { "properties": { "explanation": { "description": "One sentence explanation as to why this tool is being used, and how it contributes to the goal.", "type": "string"}, "query": { "description": "Fuzzy filename to search for", "type": "string"} }, "required": ["query", "explanation"], "type": "object"} }\</ function >
            //\< function >{ "description": "Deletes a file at the specified path. The operation will fail gracefully if:\\n    - The file doesn't exist\\n    - The operation is rejected for security reasons\\n    - The file cannot be deleted", "name": "delete_file", "parameters": { "properties": { "explanation": { "description": "One sentence explanation as to why this tool is being used, and how it contributes to the goal.", "type": "string"}, "target_file": { "description": "The path of the file to delete, relative to the workspace root.", "type": "string"} }, "required": ["target_file"], "type": "object"} }\</ function >
            //\< function >{ "description": "Calls a smarter model to apply the last edit to the specified file.\\nUse this tool immediately after the result of an edit_file tool call ONLY IF the diff is not what you expected, indicating the model applying the changes was not smart enough to follow your instructions.", "name": "reapply", "parameters": { "properties": { "target_file": { "description": "The relative path to the file to reapply the last edit to.", "type": "string"} }, "required": ["target_file"], "type": "object"} }\</ function >
            //\< function >{ "description": "When there are multiple locations that can be edited in parallel, with a similar type of edit, use this tool to sketch out a plan for the edits.\\nYou should start with the edit_plan which describes what the edits will be.\\nThen, write out the files that will be edited with the edit_files argument.\\nYou shouldn't edit more than 50 files at a time.", "name": "parallel_apply", "parameters": { "properties": { "edit_plan": { "description": "A detailed description of the parallel edits to be applied.\\nThey should be specified in a way where a model just seeing one of the files and this plan would be able to apply the edits to any of the files.\\nIt should be in the first person, describing what you will do on another iteration, after seeing the file.", "type": "string"}, "edit_regions": { "items": { "description": "The region of the file that should be edited. It should include the minimum contents needed to read in addition to the edit_plan to be able to apply the edits. You should add a lot of cushion to make sure the model definitely has the context it needs to edit the file.", "properties": { "end_line": { "description": "The end line of the region to edit. 1-indexed and inclusive.", "type": "integer"}, "relative_workspace_path": { "description": "The path to the file to edit.", "type": "string"}, "start_line": { "description": "The start line of the region to edit. 1-indexed and inclusive.", "type": "integer"} }, "required": ["relative_workspace_path"], "type": "object"}, "type": "array"} }, "required": ["edit_plan", "edit_regions"], "type": "object"} }\</ function >
            //\</ functions >

            return sb.ToString();
        }
        catch (Exception ex)
        {
            string errorMsg = $"SYSTEM: WARNING! Could not load system prompt from {SystemPromptFilePath}. Error: {ex.Message}";
            Console.WriteLine(errorMsg);
            AddChatMessage(errorMsg);
            // Return a default minimal prompt
            return "You are a helpful assistant. Format commands using json tags.";
        }
    }



}