using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Infrastructure.Identity;

public sealed class OAuthClientStore
{
    private readonly ConcurrentDictionary<string, OAuthClient> _clients = new(StringComparer.Ordinal);

    public OAuthClientStore(OAuthOptions options)
    {
        foreach (var client in options.Clients.Where(c => c.IsActive))
        {
            _clients.TryAdd(client.ClientId, client);
        }
    }

    public Task<OAuthClient?> GetClientAsync(string clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return Task.FromResult(client);
    }

    public Task<bool> ValidateSecretAsync(string clientId, string secret)
    {
        if (!_clients.TryGetValue(clientId, out var client))
        {
            return Task.FromResult(false);
        }

        var valid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(client.ClientSecret),
            Encoding.UTF8.GetBytes(secret));

        return Task.FromResult(valid);
    }

    public Task<IReadOnlyList<OAuthClient>> ListActiveClientsAsync()
    {
        IReadOnlyList<OAuthClient> list = _clients.Values.ToList();
        return Task.FromResult(list);
    }
}
