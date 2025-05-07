using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol.Transport;
using OpenCursor.Host.LlmClient;
using System.Diagnostics;
using System.IO;
using System.Windows;


namespace OpenCursor.Host
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Process _mcProcess;
        private IHost _host;
        private StreamWriter _mcInput;

        public App()
        {
           
            _host = CreateHostBuilder().Build();
        }

        public static IServiceProvider Services => ((App)Current)._host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            // Get the main window from the service provider
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            StartMcpServer();
            base.OnStartup(e);
        }


        protected override async void OnExit(ExitEventArgs e)
        {
            // Ensure the client process is terminated when the host exits
            if (_mcProcess != null && !_mcProcess.HasExited)
            {
                _mcProcess.Kill();
                _mcProcess.Dispose();
            }

            // Properly shut down the host
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder()
        {

            var builder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Register your services here
                    ConfigureServices(services);
                });

            //var mcpService = builder.AddConnectionString("OpenCursorMCPServerConnectionName");

            //var mcpServer = builder.AddProject<OpenCursor_MCPServer>("OpenCursorMCPServer")
            //    .WithReference(mcpService);


            return builder;

        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Register WPF windows
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IClientTransport>( factory =>
            {
                // Connect the MCP Server to the transport using Stdio
                var clientTransport = new StdioClientTransport(new StdioClientTransportOptions()
                {
                    Command = "OpenCursor.MCPClient.exe", // or full path
                    Name = "OpenCursor.MCPClient"
                });
                return clientTransport;
            });
            services.AddSingleton<GeminiChatClient>();

            services.AddChatClient(factory =>
            {
                var client = factory.GetRequiredService<GeminiChatClient>(); // Can easilly be replaced with a different client
                client.AsBuilder()
                .UseFunctionInvocation() // magic that makes the client call functions
                .Build();
                return client;
            });

            


        }

        // AI generated crap
        //private void StartMcpServer()
        //{
        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = "OpenCursor.MCPServer.exe", // or full path
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //        RedirectStandardInput = true,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true
        //    };

        //    _mcProcess = new Process { StartInfo = startInfo };
        //    _mcProcess.EnableRaisingEvents = true;

        //    // Handle output
        //    _mcProcess.OutputDataReceived += (sender, args) => {
        //        if (!string.IsNullOrEmpty(args.Data))
        //            Dispatcher.Invoke(() => HandleServerOutput(args.Data));
        //    };

        //    // Handle errors
        //    _mcProcess.ErrorDataReceived += (sender, args) => {
        //        if (!string.IsNullOrEmpty(args.Data))
        //            Dispatcher.Invoke(() => HandleServerError(args.Data));
        //    };

        //    _mcProcess.Start();

        //    // Set up async output reading
        //    _mcProcess.BeginOutputReadLine();
        //    _mcProcess.BeginErrorReadLine();
        //    _mcInput = _mcProcess.StandardInput;

        //    //_mcInput = _mcProcess.StandardInput;
        //}

        //private void HandleServerOutput(string data)
        //{
        //    // Process output from server
        //}

        //private void HandleServerError(string error)
        //{
        //    // Handle errors
        //}
    }

}
