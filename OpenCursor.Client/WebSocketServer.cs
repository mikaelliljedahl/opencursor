using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using OpenCursor.Client.Commands;

namespace OpenCursor.Client
{
    public class WebSocketServer
    {
        private readonly string _listeningUrl;
        private readonly LlmResponseParser _parser;
        private readonly McpProcessor _processor;
        private readonly DirectoryBrowser _browser;
        private HttpListener _listener;

        public WebSocketServer(string listeningUrl, LlmResponseParser parser, McpProcessor processor, DirectoryBrowser browser)
        {
            _listeningUrl = listeningUrl ?? throw new ArgumentNullException(nameof(listeningUrl));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        public void StartListening()
        {
            Task.Run(async () =>
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(_listeningUrl); 
                _listener.Start();
                Console.WriteLine($"\nWebSocket Response server started on {_listeningUrl.Replace("http", "ws")}");

                while (true) // Keep listening for connections (consider adding a cancellation token)
                {
                    try
                    {
                        HttpListenerContext context = await _listener.GetContextAsync();
                        if (context.Request.IsWebSocketRequest)
                        {
                            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                            Console.WriteLine("\nWebSocket connection established.");
                            // Handle client in a non-blocking way, but don't discard the task
                            // Use Task.Run or similar if HandleWebSocketClient might block significantly
                            // For now, await it directly in the loop might be okay if parsing/processing is fast
                            // but let's keep the async pattern
                            _ = HandleWebSocketClientAsync(webSocketContext.WebSocket);
                        }
                        else
                        {
                            context.Response.StatusCode = 400; // Bad Request
                            context.Response.Close();
                        }
                    }
                    catch (HttpListenerException httpEx) when (httpEx.ErrorCode == 995) // Listener stopped
                    {
                        Console.WriteLine("\nWebSocket listener stopped.");
                        break; // Exit the loop
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nError in WebSocket listener loop: {ex.Message}");
                        // Consider delay/retry or stopping listener based on error type
                        await Task.Delay(1000); // Simple delay before retrying
                    }
                }
            });
        }

        public void StopListening()
        {
            _listener?.Stop();
        }

        private async Task HandleWebSocketClientAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var currentDirectory = _browser.CurrentDirectory;

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        Console.WriteLine("WebSocket client disconnected gracefully.");
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Console Received via WebSocket: {message.Substring(0, Math.Min(message.Length, 200))}...");

                        List<IMcpCommand> commands = _parser.ParseCommands(message);

                        if (commands.Any())
                        {
                            Console.WriteLine($"Parser generated {commands.Count} command(s).");
                            _processor.ApplyMcpCommands(commands, currentDirectory);
                        }
                        else 
                        {
                            Console.WriteLine("No commands parsed from the message.");
                        }
                    }
                }
            }
            catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                Console.WriteLine("WebSocket client disconnected abruptly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling WebSocket client: {ex.Message}");
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    webSocket.Dispose();
                }
            }
        }
    }
}
