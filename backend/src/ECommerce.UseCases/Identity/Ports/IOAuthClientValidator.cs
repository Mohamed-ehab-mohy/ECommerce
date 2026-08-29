namespace ECommerce.UseCases.Identity.Ports;

public sealed record OAuthClientValidationResult(
    bool IsValid,
    IReadOnlyList<string> AllowedScopes,
    IReadOnlyList<string> AllowedGrantTypes,
    IReadOnlyList<string> RedirectUris,
    string DisplayName,
    bool RequiresSecret);

public interface IOAuthClientValidator
{
    Task<OAuthClientValidationResult?> ValidateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);

    Task<OAuthClientValidationResult?> GetClientAsync(string clientId, CancellationToken cancellationToken = default);
}
