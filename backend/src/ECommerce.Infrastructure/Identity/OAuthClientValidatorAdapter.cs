using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.Infrastructure.Identity;

public sealed class OAuthClientValidatorAdapter(OAuthClientStore store) : IOAuthClientValidator
{
    public async Task<OAuthClientValidationResult?> ValidateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var client = await store.GetClientAsync(clientId);
        return client is null || !await store.ValidateSecretAsync(clientId, clientSecret)
            ? null
            : Build(client);
    }

    public async Task<OAuthClientValidationResult?> GetClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var client = await store.GetClientAsync(clientId);
        return client is null ? null : Build(client);
    }

    private static OAuthClientValidationResult Build(OAuthClient client) => new(
        IsValid: client.IsActive,
        AllowedScopes: client.AllowedScopes,
        AllowedGrantTypes: client.AllowedGrantTypes,
        RedirectUris: client.RedirectUris,
        DisplayName: client.DisplayName,
        RequiresSecret: !string.IsNullOrEmpty(client.ClientSecret));
}
