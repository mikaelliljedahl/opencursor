using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Windows;

namespace OpenCursor.BrowserHost
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Process _clientProcess;
        private readonly IHost _host;

        public App()
        {
            _host = CreateHostBuilder().Build();
        }

        public static IServiceProvider Services => ((App)Current)._host.Services;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Ensure the client process is terminated when the host exits
            if (_clientProcess != null && !_clientProcess.HasExited)
            {
                _clientProcess.Kill();
                _clientProcess.Dispose();
            }

            // Properly shut down the host
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Register your services here
                    ConfigureServices(services);
                });

        private static void ConfigureServices(IServiceCollection services)
        {
            // Register WPF windows
            services.AddSingleton<MainWindow>();

            // Register your view models
            // services.AddSingleton<MainViewModel>();

            // Register your services, e.g. MCP-services should be moved from static creation inside the mainwindow to be places in the DI-container
            // services.AddSingleton<IYourService, YourService>();
            // services.AddScoped<IScopedService, ScopedService>();
            // services.AddTransient<ITransientService, TransientService>();
        }
    }

}
