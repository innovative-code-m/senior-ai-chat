using System.Security.Cryptography;
using Fido2NetLib;
using Fido2NetLib.Objects;
using SeniorAiChat.Api.Registration;

namespace SeniorAiChat.Api.Passkeys;

internal sealed class PasskeyAuthenticationService
{
    public const string SessionCookieName = "sac_session";

    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    private readonly object gate = new();
    private readonly IFido2 fido2;
    private readonly UserRegistrationService registrations;
    private readonly Dictionary<string, UserPasskeyRecord> passkeysByCredentialId = new();
    private readonly Dictionary<Guid, PasskeyChallengeRecord> challenges = new();
    private readonly Dictionary<string, UserSessionRecord> sessions = new();

    public PasskeyAuthenticationService(
        IFido2 fido2,
        UserRegistrationService registrations)
    {
        this.fido2 = fido2;
        this.registrations = registrations;
    }

    public PasskeyOptionsResult BeginRegistration(PasskeyEmailRequest request)
    {
        var lookup = registrations.FindByEmail(request.Email);
        if (lookup.ValidationErrors is not null)
        {
            return PasskeyOptionsResult.Validation(lookup.ValidationErrors);
        }

        if (lookup.User is null)
        {
            return PasskeyOptionsResult.Conflict("パスキー登録できる状態ではありません。");
        }

        var user = lookup.User;
        if (user.Status is not (UserStatus.PasskeyRegistrationPending or UserStatus.PasskeyResetAllowed))
        {
            return PasskeyOptionsResult.Conflict("パスキー登録できる状態ではありません。");
        }

        var existingCredentials = GetCredentialDescriptorsForUser(user.Id);
        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = CreateFidoUser(user),
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Preferred,
                UserVerification = UserVerificationRequirement.Preferred
            },
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                CredProps = true
            }
        });

        var challenge = StoreChallenge(user.Id, PasskeyChallengePurpose.Registration, options.ToJson());

        return PasskeyOptionsResult.Success(new PasskeyOptionsResponse(
            challenge.Id,
            options.ToJson()));
    }

    public async Task<PasskeyActionResult> CompleteRegistrationAsync(
        PasskeyRegistrationCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var challenge = ConsumeChallenge(request.ChallengeId, PasskeyChallengePurpose.Registration);
        if (challenge is null)
        {
            return PasskeyActionResult.Conflict("登録手続きの有効期限が切れました。もう一度やり直してください。");
        }

        var user = registrations.FindById(challenge.UserId);
        if (user is null || user.Status is not (UserStatus.PasskeyRegistrationPending or UserStatus.PasskeyResetAllowed))
        {
            return PasskeyActionResult.Conflict("パスキー登録できる状態ではありません。");
        }

        try
        {
            var options = CredentialCreateOptions.FromJson(challenge.OptionsJson);
            var result = await fido2.MakeNewCredentialAsync(
                new MakeNewCredentialParams
                {
                    AttestationResponse = request.Credential,
                    OriginalOptions = options,
                    IsCredentialIdUniqueToUserCallback = (args, _) =>
                        Task.FromResult(!CredentialExists(args.CredentialId))
                },
                cancellationToken);

            StorePasskey(user.Id, result);

            var statusResult = registrations.CompletePasskeyRegistration(user.Id);
            if (statusResult.Response is null)
            {
                return PasskeyActionResult.Conflict(
                    statusResult.ConflictMessage ?? "パスキー登録を完了できませんでした。");
            }

            return PasskeyActionResult.Success(new PasskeyActionResponse(
                user.Id,
                statusResult.Response.Status,
                statusResult.Response.Message));
        }
        catch (Exception)
        {
            return PasskeyActionResult.Conflict("端末の本人確認結果を確認できませんでした。もう一度やり直してください。");
        }
    }

    public PasskeyOptionsResult BeginLogin(PasskeyEmailRequest request)
    {
        var lookup = registrations.FindByEmail(request.Email);
        if (lookup.ValidationErrors is not null)
        {
            return PasskeyOptionsResult.Validation(lookup.ValidationErrors);
        }

        if (lookup.User is null)
        {
            return PasskeyOptionsResult.Conflict("ログインできる状態ではありません。");
        }

        var user = lookup.User;
        if (user.Status != UserStatus.Active)
        {
            return PasskeyOptionsResult.Conflict("ログインできる状態ではありません。");
        }

        var existingCredentials = GetCredentialDescriptorsForUser(user.Id);
        if (existingCredentials.Count == 0)
        {
            return PasskeyOptionsResult.Conflict("ログインできる状態ではありません。");
        }

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = existingCredentials,
            UserVerification = UserVerificationRequirement.Preferred
        });

        var challenge = StoreChallenge(user.Id, PasskeyChallengePurpose.Authentication, options.ToJson());

        return PasskeyOptionsResult.Success(new PasskeyOptionsResponse(
            challenge.Id,
            options.ToJson()));
    }

    public async Task<LoginCompleteResult> CompleteLoginAsync(
        PasskeyLoginCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var challenge = ConsumeChallenge(request.ChallengeId, PasskeyChallengePurpose.Authentication);
        if (challenge is null)
        {
            return LoginCompleteResult.Conflict("ログイン手続きの有効期限が切れました。もう一度やり直してください。");
        }

        var user = registrations.FindById(challenge.UserId);
        if (user is null || user.Status != UserStatus.Active)
        {
            return LoginCompleteResult.Conflict("ログインできる状態ではありません。");
        }

        var credentialId = ToBase64Url(request.Credential.RawId);
        var passkey = FindPasskey(credentialId);
        if (passkey is null || passkey.UserId != user.Id || passkey.RevokedAt is not null)
        {
            return LoginCompleteResult.Conflict("ログインできる状態ではありません。");
        }

        try
        {
            var options = AssertionOptions.FromJson(challenge.OptionsJson);
            var result = await fido2.MakeAssertionAsync(
                new MakeAssertionParams
                {
                    AssertionResponse = request.Credential,
                    OriginalOptions = options,
                    StoredPublicKey = passkey.PublicKey,
                    StoredSignatureCounter = passkey.SignCount,
                    IsUserHandleOwnerOfCredentialIdCallback = (args, _) =>
                        Task.FromResult(IsUserHandleOwnerOfCredential(args.UserHandle, args.CredentialId))
                },
                cancellationToken);

            UpdatePasskeyUsage(passkey.CredentialId, result.SignCount);
            var session = CreateSession(user.Id);

            return LoginCompleteResult.Success(new LoginCompleteResponse(
                user.Id,
                user.Status.ToString(),
                user.Role.ToString(),
                "ログインしました。"),
                session);
        }
        catch (Exception)
        {
            return LoginCompleteResult.Conflict("端末の本人確認結果を確認できませんでした。もう一度やり直してください。");
        }
    }

    public CurrentUserResult GetCurrentUser(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return CurrentUserResult.NoSession();
        }

        UserSessionRecord? session;
        lock (gate)
        {
            if (!sessions.TryGetValue(sessionId, out session))
            {
                return CurrentUserResult.NoSession();
            }

            if (session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                sessions.Remove(sessionId);
                return CurrentUserResult.NoSession();
            }
        }

        var user = registrations.FindById(session.UserId);
        if (user is null || user.Status != UserStatus.Active)
        {
            EndSession(sessionId);
            return CurrentUserResult.NoSession();
        }

        return CurrentUserResult.Success(new CurrentUserResponse(
            user.Id,
            user.Status.ToString(),
            user.Role.ToString()));
    }

    public void EndSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        lock (gate)
        {
            sessions.Remove(sessionId);
        }
    }

    private static Fido2User CreateFidoUser(UserRecord user)
    {
        return new Fido2User
        {
            Id = CreateUserHandle(user.Id),
            Name = user.NormalizedEmail,
            DisplayName = "senior-ai-chat 利用者"
        };
    }

    private static byte[] CreateUserHandle(Guid userId)
    {
        return System.Text.Encoding.UTF8.GetBytes(userId.ToString("N"));
    }

    private IReadOnlyList<PublicKeyCredentialDescriptor> GetCredentialDescriptorsForUser(Guid userId)
    {
        lock (gate)
        {
            return passkeysByCredentialId.Values
                .Where(passkey => passkey.UserId == userId && passkey.RevokedAt is null)
                .Select(passkey => new PublicKeyCredentialDescriptor(passkey.CredentialIdBytes))
                .ToArray();
        }
    }

    private PasskeyChallengeRecord StoreChallenge(
        Guid userId,
        PasskeyChallengePurpose purpose,
        string optionsJson)
    {
        var now = DateTimeOffset.UtcNow;
        var challenge = new PasskeyChallengeRecord(
            Guid.NewGuid(),
            userId,
            purpose,
            optionsJson,
            now,
            now.Add(ChallengeLifetime),
            null);

        lock (gate)
        {
            RemoveExpiredChallenges(now);
            challenges[challenge.Id] = challenge;
        }

        return challenge;
    }

    private PasskeyChallengeRecord? ConsumeChallenge(
        Guid challengeId,
        PasskeyChallengePurpose purpose)
    {
        var now = DateTimeOffset.UtcNow;

        lock (gate)
        {
            RemoveExpiredChallenges(now);

            if (!challenges.TryGetValue(challengeId, out var challenge))
            {
                return null;
            }

            challenges.Remove(challengeId);

            if (challenge.Purpose != purpose ||
                challenge.ExpiresAt <= now ||
                challenge.ConsumedAt is not null)
            {
                return null;
            }

            return challenge with { ConsumedAt = now };
        }
    }

    private void RemoveExpiredChallenges(DateTimeOffset now)
    {
        var expiredIds = challenges
            .Where(pair => pair.Value.ExpiresAt <= now)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var expiredId in expiredIds)
        {
            challenges.Remove(expiredId);
        }
    }

    private bool CredentialExists(byte[] credentialId)
    {
        var encoded = ToBase64Url(credentialId);

        lock (gate)
        {
            return passkeysByCredentialId.ContainsKey(encoded);
        }
    }

    private UserPasskeyRecord? FindPasskey(string credentialId)
    {
        lock (gate)
        {
            return passkeysByCredentialId.GetValueOrDefault(credentialId);
        }
    }

    private void StorePasskey(Guid userId, RegisteredPublicKeyCredential credential)
    {
        var credentialId = ToBase64Url(credential.Id);
        var now = DateTimeOffset.UtcNow;
        var passkey = new UserPasskeyRecord(
            Guid.NewGuid(),
            userId,
            credentialId,
            credential.Id,
            credential.PublicKey,
            credential.SignCount,
            credential.User.Id,
            null,
            now,
            null,
            null);

        lock (gate)
        {
            passkeysByCredentialId[credentialId] = passkey;
        }
    }

    private void UpdatePasskeyUsage(string credentialId, uint signCount)
    {
        lock (gate)
        {
            if (!passkeysByCredentialId.TryGetValue(credentialId, out var passkey))
            {
                return;
            }

            passkeysByCredentialId[credentialId] = passkey with
            {
                SignCount = signCount,
                LastUsedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private bool IsUserHandleOwnerOfCredential(byte[] userHandle, byte[] credentialId)
    {
        var encoded = ToBase64Url(credentialId);

        lock (gate)
        {
            return passkeysByCredentialId.TryGetValue(encoded, out var passkey) &&
                passkey.RevokedAt is null &&
                passkey.UserHandle.SequenceEqual(userHandle);
        }
    }

    private UserSessionRecord CreateSession(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new UserSessionRecord(
            CreateRandomToken(),
            userId,
            now,
            now.Add(SessionLifetime));

        lock (gate)
        {
            RemoveExpiredSessions(now);
            sessions[session.SessionId] = session;
        }

        return session;
    }

    private void RemoveExpiredSessions(DateTimeOffset now)
    {
        var expiredIds = sessions
            .Where(pair => pair.Value.ExpiresAt <= now)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var expiredId in expiredIds)
        {
            sessions.Remove(expiredId);
        }
    }

    private static string CreateRandomToken()
    {
        return ToBase64Url(RandomNumberGenerator.GetBytes(32));
    }

    private static string ToBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

internal sealed record PasskeyEmailRequest(string? Email);

internal sealed record PasskeyRegistrationCompleteRequest(
    Guid ChallengeId,
    AuthenticatorAttestationRawResponse Credential);

internal sealed record PasskeyLoginCompleteRequest(
    Guid ChallengeId,
    AuthenticatorAssertionRawResponse Credential);

internal sealed record PasskeyOptionsResponse(
    Guid ChallengeId,
    string OptionsJson);

internal sealed record PasskeyActionResponse(
    Guid UserId,
    string Status,
    string Message);

internal sealed record LoginCompleteResponse(
    Guid UserId,
    string Status,
    string Role,
    string Message);

internal sealed record CurrentUserResponse(
    Guid UserId,
    string Status,
    string Role);

internal sealed record PasskeyOptionsResult(
    PasskeyOptionsResponse? Response,
    IReadOnlyDictionary<string, string[]>? ValidationErrors,
    string? ConflictMessage)
{
    public static PasskeyOptionsResult Success(PasskeyOptionsResponse response)
    {
        return new PasskeyOptionsResult(response, null, null);
    }

    public static PasskeyOptionsResult Validation(IReadOnlyDictionary<string, string[]> errors)
    {
        return new PasskeyOptionsResult(null, errors, null);
    }

    public static PasskeyOptionsResult Conflict(string message)
    {
        return new PasskeyOptionsResult(null, null, message);
    }
}

internal sealed record PasskeyActionResult(
    PasskeyActionResponse? Response,
    string? ConflictMessage)
{
    public static PasskeyActionResult Success(PasskeyActionResponse response)
    {
        return new PasskeyActionResult(response, null);
    }

    public static PasskeyActionResult Conflict(string message)
    {
        return new PasskeyActionResult(null, message);
    }
}

internal sealed record LoginCompleteResult(
    LoginCompleteResponse? Response,
    UserSessionRecord? Session,
    string? ConflictMessage)
{
    public static LoginCompleteResult Success(
        LoginCompleteResponse response,
        UserSessionRecord session)
    {
        return new LoginCompleteResult(response, session, null);
    }

    public static LoginCompleteResult Conflict(string message)
    {
        return new LoginCompleteResult(null, null, message);
    }
}

internal sealed record CurrentUserResult(
    CurrentUserResponse? Response,
    bool IsUnauthorized)
{
    public static CurrentUserResult Success(CurrentUserResponse response)
    {
        return new CurrentUserResult(response, false);
    }

    public static CurrentUserResult NoSession()
    {
        return new CurrentUserResult(null, true);
    }
}

internal sealed record UserPasskeyRecord(
    Guid Id,
    Guid UserId,
    string CredentialId,
    byte[] CredentialIdBytes,
    byte[] PublicKey,
    uint SignCount,
    byte[] UserHandle,
    string? DeviceName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);

internal sealed record PasskeyChallengeRecord(
    Guid Id,
    Guid UserId,
    PasskeyChallengePurpose Purpose,
    string OptionsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt);

internal sealed record UserSessionRecord(
    string SessionId,
    Guid UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

internal enum PasskeyChallengePurpose
{
    Registration,
    Authentication
}
