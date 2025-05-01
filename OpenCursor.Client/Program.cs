using OpenCursor.Client;


const string WebSocketUrl = "http://localhost:12346/"; // Listener uses HTTP prefix

// Set up
Console.CursorVisible = false;

var browser = new DirectoryBrowser();
var ui = new UIRenderer(browser);
var navigator = new KeyboardNavigator(browser, ui);

// Instantiate McpProcessor, LlmResponseParser, and WebSocketServer
var mcpProcessor = new McpProcessor(Directory.GetCurrentDirectory()); 
var llmParser = new LlmResponseParser(); 
var webSocketServer = new WebSocketServer(WebSocketUrl, llmParser, mcpProcessor, browser);

// Start WebSocket server
webSocketServer.StartListening(); 

// Start
browser.LoadDirectory(Directory.GetCurrentDirectory());

bool running = true;
while (running)
{
    ui.Draw();
    running = navigator.HandleKeyPress();
}
