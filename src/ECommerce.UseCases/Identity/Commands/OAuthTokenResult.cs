namespace ECommerce.UseCases.Identity.Commands;

public sealed record OAuthTokenResult(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    string Scope);
