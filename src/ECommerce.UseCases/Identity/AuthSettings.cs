namespace ECommerce.UseCases.Identity;

public sealed record AuthSettings
{
    public const string SectionName = "Auth";

    public bool RequireVerifiedEmail { get; init; } = true;

    public int MaxFailedLoginAttempts { get; init; } = 5;

    public int LockoutDurationMinutes { get; init; } = 15;
}
