using OpenCursor.Client;
using System.Net;
using System.Net.WebSockets;
using System.Text;
// Re-evaluate necessary usings once the correct SDK API is known
// using ModelContextProtocol.Parsing; 

// *** Added: Define WebSocket server URL ***
const string WebSocketUrl = "http://localhost:12346/"; // Listener uses HTTP prefix

// Set up
Console.CursorVisible = false;

var browser = new DirectoryBrowser();
var ui = new UIRenderer(browser);
var navigator = new KeyboardNavigator(browser, ui);

// Instantiate McpProcessor, LlmResponseParser, and WebSocketServer
var mcpProcessor = new McpProcessor(); 
var llmParser = new LlmResponseParser(); 
var webSocketServer = new WebSocketServer(WebSocketUrl, llmParser, mcpProcessor, browser);

// Start WebSocket server
webSocketServer.StartListening(); 

// Start
browser.LoadDirectory(Directory.GetCurrentDirectory());
// await Task.Delay(5000); // Need to load web broswer before we can send system prompt
// await LLMClient.PrepareAndSendSystemPrompt();

bool running = true;
while (running)
{
    ui.Draw();
    running = navigator.HandleKeyPress();
}
