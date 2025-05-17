using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;


var builder = Host.CreateEmptyApplicationBuilder(settings: null);

//builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Information)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();


var app = builder.Build();


await app.RunAsync();