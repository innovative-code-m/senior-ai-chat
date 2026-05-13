using System.Net.Mail;

namespace SeniorAiChat.Api.Registration;

internal sealed class UserRegistrationService
{
    private readonly object gate = new();
    private readonly List<UserRecord> users = [];

    public RegistrationCreateResult Register(RegistrationRequest request)
    {
        var normalized = NormalizeEmail(request.Email);
        var errors = ValidateRegistration(request, normalized);

        if (errors.Count > 0)
        {
            return RegistrationCreateResult.Validation(errors);
        }

        lock (gate)
        {
            var existing = users.FirstOrDefault(user => user.NormalizedEmail == normalized);
            if (existing is not null)
            {
                return RegistrationCreateResult.Conflict(CreateDuplicateMessage(existing.Status));
            }

            var now = DateTimeOffset.UtcNow;
            var user = new UserRecord
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName!.Trim(),
                Email = request.Email!.Trim(),
                NormalizedEmail = normalized!,
                GraduationClassName = request.GraduationClassName!.Trim(),
                Status = UserStatus.PendingApproval,
                Role = UserRole.Member,
                CreatedAt = now,
                UpdatedAt = now
            };

            users.Add(user);

            return RegistrationCreateResult.Success(new RegistrationResponse(
                user.Id,
                user.Status.ToString(),
                user.CreatedAt,
                "仮登録を受け付けました。管理者の確認をお待ちください。"));
        }
    }

    public RegistrationStatusResult GetStatus(string? email)
    {
        var normalized = NormalizeEmail(email);
        var errors = ValidateEmailOnly(normalized);

        if (errors.Count > 0)
        {
            return RegistrationStatusResult.Validation(errors);
        }

        lock (gate)
        {
            var user = users.FirstOrDefault(candidate => candidate.NormalizedEmail == normalized);
            if (user is null)
            {
                return RegistrationStatusResult.NotFound();
            }

            return RegistrationStatusResult.Success(new RegistrationStatusResponse(
                user.Status.ToString(),
                CreateStatusMessage(user.Status)));
        }
    }

    public IReadOnlyList<PendingUserResponse> GetPendingUsers()
    {
        lock (gate)
        {
            return users
                .Where(user => user.Status == UserStatus.PendingApproval)
                .OrderBy(user => user.CreatedAt)
                .Select(user => new PendingUserResponse(
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.GraduationClassName,
                    user.Status.ToString(),
                    user.CreatedAt))
                .ToArray();
        }
    }

    public AdminUserActionResult Approve(Guid id)
    {
        lock (gate)
        {
            var user = users.FirstOrDefault(candidate => candidate.Id == id);
            if (user is null)
            {
                return AdminUserActionResult.NotFound("指定されたユーザーは見つかりませんでした。");
            }

            if (user.Status != UserStatus.PendingApproval)
            {
                return AdminUserActionResult.Conflict(CreateInvalidTransitionMessage(user.Status));
            }

            var now = DateTimeOffset.UtcNow;
            user.Status = UserStatus.PasskeyRegistrationPending;
            user.ApprovedAt = now;
            user.UpdatedAt = now;

            return AdminUserActionResult.Success(new AdminUserActionResponse(
                user.Id,
                user.Status.ToString(),
                user.UpdatedAt,
                "承認しました。利用者はパスキー登録待ちの状態になりました。"));
        }
    }

    public AdminUserActionResult Reject(Guid id)
    {
        lock (gate)
        {
            var user = users.FirstOrDefault(candidate => candidate.Id == id);
            if (user is null)
            {
                return AdminUserActionResult.NotFound("指定されたユーザーは見つかりませんでした。");
            }

            if (user.Status != UserStatus.PendingApproval)
            {
                return AdminUserActionResult.Conflict(CreateInvalidTransitionMessage(user.Status));
            }

            var now = DateTimeOffset.UtcNow;
            user.Status = UserStatus.Rejected;
            user.RejectedAt = now;
            user.UpdatedAt = now;

            return AdminUserActionResult.Success(new AdminUserActionResponse(
                user.Id,
                user.Status.ToString(),
                user.UpdatedAt,
                "否認しました。"));
        }
    }

    private static string? NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? null
            : trimmed.ToLowerInvariant();
    }

    private static Dictionary<string, string[]> ValidateRegistration(
        RegistrationRequest request,
        string? normalizedEmail)
    {
        var errors = ValidateEmailOnly(normalizedEmail);

        AddRequiredTextError(errors, "fullName", request.FullName, "氏名を入力してください。", 80);
        AddRequiredTextError(errors, "graduationClassName", request.GraduationClassName, "卒業時のクラス名を入力してください。", 40);

        return errors;
    }

    private static Dictionary<string, string[]> ValidateEmailOnly(string? normalizedEmail)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            errors["email"] = ["メールアドレスを入力してください。"];
            return errors;
        }

        if (normalizedEmail.Length > 254 || !IsValidEmail(normalizedEmail))
        {
            errors["email"] = ["メールアドレスの形式を確認してください。"];
        }

        return errors;
    }

    private static void AddRequiredTextError(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        string requiredMessage,
        int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            errors[key] = [requiredMessage];
            return;
        }

        if (trimmed.Length > maxLength)
        {
            errors[key] = [$"{maxLength}文字以内で入力してください。"];
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string CreateDuplicateMessage(UserStatus status)
    {
        return status switch
        {
            UserStatus.PendingApproval => "このメールアドレスはすでに承認待ちです。状態確認をご利用ください。",
            UserStatus.PasskeyRegistrationPending => "このメールアドレスは承認済みです。状態確認をご利用ください。",
            UserStatus.Rejected => "このメールアドレスでの再申請は、この Phase では管理者への個別確認が必要です。",
            UserStatus.Suspended => "このメールアドレスは停止中です。管理者へ個別に確認してください。",
            UserStatus.PasskeyResetAllowed => "このメールアドレスはパスキー再登録の手続き中です。",
            _ => "このメールアドレスはすでに登録されています。"
        };
    }

    private static string CreateStatusMessage(UserStatus status)
    {
        return status switch
        {
            UserStatus.PendingApproval => "管理者の確認待ちです。",
            UserStatus.PasskeyRegistrationPending => "承認済みです。パスキー登録は次の Phase で扱います。",
            UserStatus.Rejected => "参加を許可できませんでした。必要な場合は管理者へ確認してください。",
            UserStatus.Suspended => "利用停止中です。管理者へ確認してください。",
            UserStatus.PasskeyResetAllowed => "パスキー再登録の許可中です。",
            UserStatus.Active => "利用可能な状態です。",
            _ => "状態を確認できませんでした。"
        };
    }

    private static string CreateInvalidTransitionMessage(UserStatus status)
    {
        return $"現在の状態は {status} です。承認待ちの利用者だけを操作できます。";
    }
}

internal sealed record RegistrationRequest(
    string? FullName,
    string? Email,
    string? GraduationClassName);

internal sealed record RegistrationResponse(
    Guid Id,
    string Status,
    DateTimeOffset SubmittedAt,
    string Message);

internal sealed record RegistrationStatusResponse(
    string Status,
    string Message);

internal sealed record PendingUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string GraduationClassName,
    string Status,
    DateTimeOffset CreatedAt);

internal sealed record AdminUserActionResponse(
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt,
    string Message);

internal sealed record RegistrationCreateResult(
    RegistrationResponse? Response,
    IReadOnlyDictionary<string, string[]>? ValidationErrors,
    string? ConflictMessage)
{
    public static RegistrationCreateResult Success(RegistrationResponse response)
    {
        return new RegistrationCreateResult(response, null, null);
    }

    public static RegistrationCreateResult Validation(IReadOnlyDictionary<string, string[]> errors)
    {
        return new RegistrationCreateResult(null, errors, null);
    }

    public static RegistrationCreateResult Conflict(string message)
    {
        return new RegistrationCreateResult(null, null, message);
    }
}

internal sealed record RegistrationStatusResult(
    RegistrationStatusResponse? Response,
    IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static RegistrationStatusResult Success(RegistrationStatusResponse response)
    {
        return new RegistrationStatusResult(response, null);
    }

    public static RegistrationStatusResult Validation(IReadOnlyDictionary<string, string[]> errors)
    {
        return new RegistrationStatusResult(null, errors);
    }

    public static RegistrationStatusResult NotFound()
    {
        return new RegistrationStatusResult(null, null);
    }
}

internal sealed record AdminUserActionResult(
    AdminUserActionResponse? Response,
    string? NotFoundMessage,
    string? ConflictMessage)
{
    public static AdminUserActionResult Success(AdminUserActionResponse response)
    {
        return new AdminUserActionResult(response, null, null);
    }

    public static AdminUserActionResult NotFound(string message)
    {
        return new AdminUserActionResult(null, message, null);
    }

    public static AdminUserActionResult Conflict(string message)
    {
        return new AdminUserActionResult(null, null, message);
    }
}
