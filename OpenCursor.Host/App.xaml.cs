using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace OpenCursor.BrowserHost
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Process _clientProcess;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //try
            //{
            //    // Path to the OpenCursor.Client executable
            //    string serverExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenCursor.MCPServer.exe");

            //    if (!File.Exists(serverExecutablePath))
            //    {
            //        MessageBox.Show($"Client executable not found at: {serverExecutablePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //        Shutdown();
            //        return;
            //    }

            //    // Start the OpenCursor.Client process
            //    _clientProcess = new Process
            //    {
            //        StartInfo = new ProcessStartInfo
            //        {
            //            FileName = serverExecutablePath,
            //            UseShellExecute = false,
            //            CreateNoWindow = true
            //        }
            //    };
            //    _clientProcess.Start();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Failed to start OpenCursor.Client: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    Shutdown();
            //}
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Ensure the client process is terminated when the host exits
            if (_clientProcess != null && !_clientProcess.HasExited)
            {
                _clientProcess.Kill();
                _clientProcess.Dispose();
            }
        }
    }

}
