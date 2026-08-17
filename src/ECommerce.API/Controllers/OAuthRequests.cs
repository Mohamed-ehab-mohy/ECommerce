namespace ECommerce.API.Controllers;

public sealed record OAuthTokenRequest(
    string GrantType,
    string? Code,
    string? RedirectUri,
    string? ClientId,
    string? ClientSecret,
    string? Username,
    string? Password,
    string? Scope);

public sealed record OAuthRevokeRequest(string Token, string? TokenHint);
