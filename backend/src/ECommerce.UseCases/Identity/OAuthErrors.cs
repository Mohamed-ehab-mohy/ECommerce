
namespace ECommerce.UseCases.Identity;

public static class OAuthErrors
{
    public static readonly Error InvalidClient = new(
        "ERR_OAUTH_001",
        "Invalid client credentials.",
        ErrorType.Unauthorized);

    public static readonly Error UnauthorizedGrantType = new(
        "ERR_OAUTH_002",
        "Client is not authorized for this grant type.",
        ErrorType.Unauthorized);

    public static readonly Error InvalidGrant = new(
        "ERR_OAUTH_003",
        "Invalid grant. Verify required parameters.",
        ErrorType.BadRequest);

    public static readonly Error InvalidScope = new(
        "ERR_OAUTH_004",
        "No requested scopes are allowed for this client.",
        ErrorType.BadRequest);

    public static readonly Error InvalidCredentials = new(
        "ERR_OAUTH_005",
        "Invalid username or password.",
        ErrorType.Unauthorized);

    public static Error AccountLocked(int retryAfterSeconds) => new Error(
        "ERR_OAUTH_006",
        $"Account is temporarily locked. Try again in {retryAfterSeconds} seconds.",
        ErrorType.TooManyRequests)
        .With(new Dictionary<string, object?> { ["retry_after_seconds"] = retryAfterSeconds });

    public static Error TooManyAttempts(int retryAfterSeconds) => new Error(
        "ERR_OAUTH_007",
        $"Too many failed attempts. Try again in {retryAfterSeconds} seconds.",
        ErrorType.TooManyRequests)
        .With(new Dictionary<string, object?> { ["retry_after_seconds"] = retryAfterSeconds });

    public static readonly Error InvalidRedirectUri = new(
        "ERR_OAUTH_006",
        "The provided redirect_uri is not registered for this client.",
        ErrorType.BadRequest);

    public static readonly Error InvalidCodeChallenge = new(
        "ERR_OAUTH_007",
        "Invalid or unsupported code challenge.",
        ErrorType.BadRequest);

    public static readonly Error InvalidPkceVerifier = new(
        "ERR_OAUTH_008",
        "PKCE verification failed.",
        ErrorType.BadRequest);
}
