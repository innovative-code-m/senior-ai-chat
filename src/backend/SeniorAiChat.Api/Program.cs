using Microsoft.AspNetCore.SignalR;
using SeniorAiChat.Api.Registration;

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

builder.Services.AddSingleton<UserRegistrationService>();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () =>
{
    var response = new HealthResponse(
        Status: "ok",
        Service: "SeniorAiChat.Api",
        Phase: "Phase 2",
        SignalRHub: "/hubs/chat");

    return Results.Ok(response);
});

app.MapPost("/api/registrations", (
    RegistrationRequest request,
    UserRegistrationService registrations) =>
{
    var result = registrations.Register(request);

    if (result.ValidationErrors is not null)
    {
        return Results.ValidationProblem(
            result.ValidationErrors,
            title: "入力内容を確認してください",
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (result.ConflictMessage is not null)
    {
        return Results.Problem(
            title: "仮登録できません",
            detail: result.ConflictMessage,
            statusCode: StatusCodes.Status409Conflict);
    }

    return Results.Created(
        $"/api/registrations/status?email={Uri.EscapeDataString(request.Email ?? string.Empty)}",
        result.Response);
});

app.MapGet("/api/registrations/status", (
    string? email,
    UserRegistrationService registrations) =>
{
    var result = registrations.GetStatus(email);

    if (result.ValidationErrors is not null)
    {
        return Results.ValidationProblem(
            result.ValidationErrors,
            title: "入力内容を確認してください",
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (result.Response is null)
    {
        return Results.Problem(
            title: "申請が見つかりません",
            detail: "入力したメールアドレスの仮登録は見つかりませんでした。",
            statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Ok(result.Response);
});

if (app.Environment.IsDevelopment())
{
    var admin = app.MapGroup("/api/admin")
        .WithTags("Development admin");

    admin.MapGet("/users/pending", (UserRegistrationService registrations) =>
    {
        return Results.Ok(registrations.GetPendingUsers());
    });

    admin.MapPost("/users/{id:guid}/approve", (
        Guid id,
        UserRegistrationService registrations) =>
    {
        var result = registrations.Approve(id);
        return ToAdminActionResult(result);
    });

    admin.MapPost("/users/{id:guid}/reject", (
        Guid id,
        UserRegistrationService registrations) =>
    {
        var result = registrations.Reject(id);
        return ToAdminActionResult(result);
    });
}

// Phase 2 でも SignalR は接続口だけを維持し、チャット機能は後続 Phase で実装する。
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

static IResult ToAdminActionResult(AdminUserActionResult result)
{
    if (result.Response is not null)
    {
        return Results.Ok(result.Response);
    }

    if (result.NotFoundMessage is not null)
    {
        return Results.Problem(
            title: "対象ユーザーが見つかりません",
            detail: result.NotFoundMessage,
            statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Problem(
        title: "状態を変更できません",
        detail: result.ConflictMessage,
        statusCode: StatusCodes.Status409Conflict);
}

internal sealed record HealthResponse(
    string Status,
    string Service,
    string Phase,
    string SignalRHub);

internal sealed class ChatHub : Hub
{
}
