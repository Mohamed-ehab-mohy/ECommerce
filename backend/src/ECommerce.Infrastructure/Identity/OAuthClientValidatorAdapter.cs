using ECommerce.Infrastructure.Identity;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.Infrastructure.Identity;

public sealed class OAuthClientValidatorAdapter(OAuthClientStore store) : IOAuthClientValidator
{
    public async Task<OAuthClientValidationResult?> ValidateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var client = await store.GetClientAsync(clientId);
        return client is null || !await store.ValidateSecretAsync(clientId, clientSecret)
            ? null
            : new OAuthClientValidationResult(
            IsValid: true,
            AllowedScopes: client.AllowedScopes,
            AllowedGrantTypes: client.AllowedGrantTypes);
    }
}
