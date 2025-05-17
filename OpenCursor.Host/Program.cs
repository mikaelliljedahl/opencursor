using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Transport;
using OpenCursor.Host.LlmClient;
using Serilog;

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

builder.Services.AddSingleton<IClientTransport>(factory =>
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
builder.Services.AddSingleton<WrappedGeminiChatClient>();
builder.Services.AddSingleton<OpenRouterChatClient>();

builder.Services.AddChatClient(factory =>
{
    var client = factory.GetRequiredService<OpenRouterChatClient>(); // Can easilly be replaced with a different client
    return client.AsBuilder()
    .UseFunctionInvocation() // magic that makes the client call functions
    .Build();
});


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