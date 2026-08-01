using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Identity;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new(
        "ERR_AUTH_001",
        "Invalid email or password.",
        ErrorType.Unauthorized);

    public static readonly Error RefreshTokenInvalid = new(
        "ERR_AUTH_002",
        "The refresh token is invalid or has expired.",
        ErrorType.Unauthorized);

    public static readonly Error RefreshTokenReused = new(
        "ERR_AUTH_002",
        "Refresh token reuse detected; the token family has been revoked.",
        ErrorType.Unauthorized);

    public static Error AccountLocked(int retryAfterSeconds) => new(
        "ERR_AUTH_003",
        "Account is locked due to too many failed attempts.",
        ErrorType.Locked,
        retryAfterSeconds);

    public static Error TooManyAttempts(int retryAfterSeconds) => new(
        "ERR_AUTH_005",
        "Too many authentication attempts from this address.",
        ErrorType.TooManyRequests,
        retryAfterSeconds);
}
