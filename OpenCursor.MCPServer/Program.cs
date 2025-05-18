//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Serilog;


//var builder = Host.CreateEmptyApplicationBuilder(settings: null);

////builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Debug()
//    .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Debug)
//    .WriteTo.File("logs/mcpserverlog.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
//    .CreateLogger();

//builder.Logging.AddDebug();
//builder.Logging.AddConsole();
//builder.Logging.SetMinimumLevel(LogLevel.Debug);
//builder.Logging.AddSerilog();

//builder.Services
//    .AddMcpServer()
//    .WithStdioServerTransport()
//    .WithToolsFromAssembly();


//var app = builder.Build();


//await app.RunAsync();