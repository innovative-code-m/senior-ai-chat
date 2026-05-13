using Fido2NetLib;
using Microsoft.AspNetCore.SignalR;
using SeniorAiChat.Api.Passkeys;
using SeniorAiChat.Api.Registration;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Passkeys:Origins")
    .GetChildren()
    .Select(origin => origin.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Cast<string>()
    .ToArray();

if (allowedOrigins.Length == 0)
{
    allowedOrigins =
    [
        "http://localhost:5086",
        "http://localhost:5173",
        "http://127.0.0.1:5173"
    ];
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<UserRegistrationService>();
builder.Services.AddSingleton(new Fido2Configuration
{
    ServerDomain = builder.Configuration["Passkeys:RelyingPartyId"] ?? "localhost",
    ServerName = builder.Configuration["Passkeys:RelyingPartyName"] ?? "senior-ai-chat",
    Origins = new HashSet<string>(allowedOrigins),
    Timeout = 300_000
});
builder.Services.AddSingleton<IMetadataService, LocalMetadataService>();
builder.Services.AddSingleton<IFido2>(services => new Fido2(
    services.GetRequiredService<Fido2Configuration>(),
    services.GetRequiredService<IMetadataService>()));
builder.Services.AddSingleton<PasskeyAuthenticationService>();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () =>
{
    var response = new HealthResponse(
        Status: "ok",
        Service: "SeniorAiChat.Api",
        Phase: "Phase 3",
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

app.MapPost("/api/passkeys/register/options", (
    PasskeyEmailRequest request,
    PasskeyAuthenticationService passkeys) =>
{
    var result = passkeys.BeginRegistration(request);
    return ToPasskeyOptionsResult(result, "パスキー登録を開始できません");
});

app.MapPost("/api/passkeys/register/complete", async (
    PasskeyRegistrationCompleteRequest request,
    PasskeyAuthenticationService passkeys,
    CancellationToken cancellationToken) =>
{
    var result = await passkeys.CompleteRegistrationAsync(request, cancellationToken);
    return ToPasskeyActionResult(result, "パスキー登録を完了できません");
});

app.MapPost("/api/auth/passkey/options", (
    PasskeyEmailRequest request,
    PasskeyAuthenticationService passkeys) =>
{
    var result = passkeys.BeginLogin(request);
    return ToPasskeyOptionsResult(result, "ログインを開始できません");
});

app.MapPost("/api/auth/passkey/complete", async (
    HttpContext httpContext,
    PasskeyLoginCompleteRequest request,
    PasskeyAuthenticationService passkeys,
    CancellationToken cancellationToken) =>
{
    var result = await passkeys.CompleteLoginAsync(request, cancellationToken);
    if (result.Response is null || result.Session is null)
    {
        return Results.Problem(
            title: "ログインできません",
            detail: result.ConflictMessage,
            statusCode: StatusCodes.Status409Conflict);
    }

    AppendSessionCookie(httpContext, result.Session);
    return Results.Ok(result.Response);
});

app.MapGet("/api/auth/me", (
    HttpContext httpContext,
    PasskeyAuthenticationService passkeys) =>
{
    var result = passkeys.GetCurrentUser(
        httpContext.Request.Cookies[PasskeyAuthenticationService.SessionCookieName]);

    return result.Response is null
        ? Results.Unauthorized()
        : Results.Ok(result.Response);
});

app.MapPost("/api/auth/logout", (
    HttpContext httpContext,
    PasskeyAuthenticationService passkeys) =>
{
    passkeys.EndSession(
        httpContext.Request.Cookies[PasskeyAuthenticationService.SessionCookieName]);
    ClearSessionCookie(httpContext);

    return Results.Ok(new LogoutResponse("ログアウトしました。"));
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

static IResult ToPasskeyOptionsResult(PasskeyOptionsResult result, string title)
{
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
            title: title,
            detail: result.ConflictMessage,
            statusCode: StatusCodes.Status409Conflict);
    }

    var body = $$"""
        {"challengeId":"{{result.Response.ChallengeId}}","publicKey":{{result.Response.OptionsJson}}}
        """;

    return Results.Content(body, "application/json");
}

static IResult ToPasskeyActionResult(PasskeyActionResult result, string title)
{
    if (result.Response is not null)
    {
        return Results.Ok(result.Response);
    }

    return Results.Problem(
        title: title,
        detail: result.ConflictMessage,
        statusCode: StatusCodes.Status409Conflict);
}

static void AppendSessionCookie(HttpContext httpContext, UserSessionRecord session)
{
    httpContext.Response.Cookies.Append(
        PasskeyAuthenticationService.SessionCookieName,
        session.SessionId,
        CreateSessionCookieOptions(httpContext, session.ExpiresAt));
}

static void ClearSessionCookie(HttpContext httpContext)
{
    httpContext.Response.Cookies.Delete(
        PasskeyAuthenticationService.SessionCookieName,
        CreateSessionCookieOptions(httpContext, DateTimeOffset.UnixEpoch));
}

static CookieOptions CreateSessionCookieOptions(
    HttpContext httpContext,
    DateTimeOffset expiresAt)
{
    return new CookieOptions
    {
        HttpOnly = true,
        Secure = httpContext.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expiresAt
    };
}

internal sealed record HealthResponse(
    string Status,
    string Service,
    string Phase,
    string SignalRHub);

internal sealed record LogoutResponse(string Message);

internal sealed class ChatHub : Hub
{
}
