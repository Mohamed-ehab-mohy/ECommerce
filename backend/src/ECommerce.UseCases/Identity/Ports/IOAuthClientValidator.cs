namespace ECommerce.UseCases.Identity.Ports;

public sealed record OAuthClientValidationResult(
    bool IsValid,
    IReadOnlyList<string> AllowedScopes,
    IReadOnlyList<string> AllowedGrantTypes);

public interface IOAuthClientValidator
{
    Task<OAuthClientValidationResult?> ValidateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
}
