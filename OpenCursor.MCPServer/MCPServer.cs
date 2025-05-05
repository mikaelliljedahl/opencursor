using OpenCursor.Client;

namespace OpenCursor.MCPServer;

public class MCPServer
{
    //const string WebSocketUrl = "http://localhost:12346/"; // Listener uses HTTP prefix
    private readonly UIRenderer ui;
    private readonly KeyboardNavigator navigator;

    public static string WorkspaceRoot { get; internal set; }


    public MCPServer()
    {

        //// Set up
        //Console.CursorVisible = false;

        //var browser = new DirectoryBrowser();
        //ui = new UIRenderer(browser);
        //navigator = new KeyboardNavigator(browser, ui);

        // Instantiate McpProcessor, LlmResponseParser, and WebSocketServer
        //var mcpProcessor = new McpProcessor(Directory.GetCurrentDirectory());
        //var llmParser = new LlmResponseParser();
        //var webSocketServer = new WebSocketServer(WebSocketUrl, llmParser, mcpProcessor, browser);

        // Start WebSocket server
        //webSocketServer.StartListening();

        // Start
        //browser.LoadDirectory(Directory.GetCurrentDirectory());


    }


    // method to start the console application, this will be obsolete since we will move directory browsing to the WPF-host UI
    //public void StartConsole()
    //{
    //    bool running = true;
    //    while (running)
    //    {
    //        ui.Draw();
    //        running = navigator.HandleKeyPress();

    //    }
    //}

}