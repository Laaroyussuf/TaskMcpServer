using TaskMcpServer.Services;
using TaskMcpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TaskService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<TaskTools>();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
