namespace SeniorAiChat.Api.Registration;

internal sealed class UserRecord
{
    public required Guid Id { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string NormalizedEmail { get; init; }

    public required string GraduationClassName { get; init; }

    public required UserStatus Status { get; set; }

    public required UserRole Role { get; init; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; set; }
}

internal enum UserStatus
{
    PendingApproval,
    PasskeyRegistrationPending,
    Active,
    Suspended,
    PasskeyResetAllowed,
    Rejected
}

internal enum UserRole
{
    Member,
    Admin
}
