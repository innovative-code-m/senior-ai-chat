using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://127.0.0.1:5173", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () =>
{
    var response = new HealthResponse(
        Status: "ok",
        Service: "SeniorAiChat.Api",
        Phase: "Phase 1",
        SignalRHub: "/hubs/chat");

    return Results.Ok(response);
});

// Phase 1 では SignalR の接続口だけを用意し、チャット機能は後続 Phase で実装する。
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

internal sealed record HealthResponse(
    string Status,
    string Service,
    string Phase,
    string SignalRHub);

internal sealed class ChatHub : Hub
{
}
