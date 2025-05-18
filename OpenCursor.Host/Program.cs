using Microsoft.Extensions.AI;
using OpenCursor.Host;
using OpenCursor.Host.LlmClient;
using Serilog;
using OpenCursor.MCPServer.Tools; // For ReadFileTool
using ModelContextProtocol.Server; // For AddMcpServer

var builder = WebApplication.CreateBuilder(args);

// Configure logging
if (builder.Environment.IsDevelopment())
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(standardErrorFromLevel: Serilog.Events.LogEventLevel.Debug)
        .WriteTo.File("logs/hostlog.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
        .CreateLogger();

    builder.Logging.AddDebug();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Logging.AddSerilog();
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register MCP server in-process and only the ReadFileTool
builder.Services.AddMcpServer()
    .WithToolsFromAssembly(typeof(ReadFileTool).Assembly);

// Register settings and chat client selector services
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<ChatClientSelectorService>();

// Register McpClientService as a singleton
builder.Services.AddScoped<McpClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();