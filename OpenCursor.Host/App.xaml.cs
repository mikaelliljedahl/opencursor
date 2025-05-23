﻿using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol.Transport;
using OpenCursor.Host.LlmClient;
using Serilog;
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

            


            //StartMcpServer();
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
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // This automatically loads:
                    // - appsettings.json
                    // - appsettings.{Environment}.json
                    // - Environment variables
                    // - Command line arguments
                    config.SetBasePath(Directory.GetCurrentDirectory());

                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register your services here
                    ConfigureServices(services);
                });

            return builder;
        }

        private static void ConfigureServices(IServiceCollection services)
        {

            if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Debug)
                    .WriteTo.File("logs/hostlog.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
                    .CreateLogger();

                // Configure logging
                services.AddLogging(configure =>
                {
                    configure.AddDebug(); // This sends logs to the debug output window
                    configure.AddConsole(); // This sends logs to the console output
                    configure.SetMinimumLevel(LogLevel.Debug); // Set the minimum log level
                    configure.AddSerilog();
                });
            }

            // Register WPF windows
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IClientTransport>( factory =>
            {
                // Connect to the MCP Server process
                var clientTransport = new StdioClientTransport(new StdioClientTransportOptions()
                {
                    Command = "OpenCursor.MCPServer.exe", // Must match server's executable name
                    Name = "OpenCursor.MCPServer"
                });
                return clientTransport;
            });
           
            // Register chat client with function invocation support
            services.AddSingleton<WrappedGeminiChatClient>();
            services.AddSingleton<OpenRouterChatClient>();

            services.AddChatClient(factory =>
            {
                var client = factory.GetRequiredService<OpenRouterChatClient>(); // Can easilly be replaced with a different client
                return client.AsBuilder()
                .UseFunctionInvocation() // magic that makes the client call functions
                .Build();
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
