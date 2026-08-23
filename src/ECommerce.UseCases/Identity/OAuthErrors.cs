
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
}
