using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCursor.Host
{
    public class McpServerHostedService : IHostedService
    {
        private Process _mcProcess;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly TaskCompletionSource<bool> _serverReadyTcs = new TaskCompletionSource<bool>();

        public Task WaitForServerReadyAsync() => _serverReadyTcs.Task;

        public McpServerHostedService(IHostApplicationLifetime appLifetime)
        {
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnApplicationStarted);
            _appLifetime.ApplicationStopping.Register(OnApplicationStopping);

            // The MCP server process will be started when the application has fully started
            return Task.CompletedTask;
        }

        private void OnApplicationStarted()
        {
            // Start the MCP Server process
            var startInfo = new ProcessStartInfo
            {
                FileName = "OpenCursor.MCPServer.exe", // Must match server's executable name
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory // Set working directory to where the exe is
            };

            _mcProcess = new Process { StartInfo = startInfo };
            _mcProcess.EnableRaisingEvents = true;

            // Optional: Handle output and errors if needed for logging or debugging
            _mcProcess.OutputDataReceived += (sender, args) => {
                if (!string.IsNullOrEmpty(args.Data))
                    Console.WriteLine($"MCP_SERVER_OUT: {args.Data}");
            };
            _mcProcess.ErrorDataReceived += (sender, args) => {
                if (!string.IsNullOrEmpty(args.Data))
                    Console.Error.WriteLine($"MCP_SERVER_ERR: {args.Data}");
            };

            try
            {
                _mcProcess.Start();
                _mcProcess.BeginOutputReadLine();
                _mcProcess.BeginErrorReadLine();
                Console.WriteLine("MCP Server process started.");
                _serverReadyTcs.SetResult(true); // Signal that the server is ready
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error starting MCP Server process: {ex.Message}");
                _serverReadyTcs.SetException(ex); // Signal failure
                // Consider stopping the application if the server fails to start
                _appLifetime.StopApplication();
            }
        }

        private void OnApplicationStopping()
        {
            // Ensure the client process is terminated when the host exits
            if (_mcProcess != null && !_mcProcess.HasExited)
            {
                try
                {
                    _mcProcess.Kill(true); // Kill the process and its descendants
                    Console.WriteLine("MCP Server process terminated.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error terminating MCP Server process: {ex.Message}");
                }
            }
            _mcProcess?.Dispose();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // OnApplicationStopping handles the cleanup
            return Task.CompletedTask;
        }
    }
}