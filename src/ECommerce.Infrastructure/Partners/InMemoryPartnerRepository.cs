using System.Collections.Concurrent;
using ECommerce.UseCases.Partners;

namespace ECommerce.Infrastructure.Partners;

public sealed class InMemoryPartnerRepository : IPartnerRepository
{
    private readonly ConcurrentDictionary<string, PartnerApiKey> _keys = new();
    private readonly ConcurrentDictionary<Guid, PartnerAccount> _accounts = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _usage = new();

    public Task<PartnerApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken)
    {
        _keys.TryGetValue(keyHash, out var key);
        return Task.FromResult(key);
    }

    public Task<PartnerAccount?> GetByIdAsync(Guid partnerId, CancellationToken cancellationToken)
    {
        _accounts.TryGetValue(partnerId, out var account);
        return Task.FromResult(account);
    }

    public Task RecordUsageAsync(Guid apiKeyId, DateTime utcNow, CancellationToken cancellationToken)
    {
        _usage[apiKeyId] = utcNow;
        return Task.CompletedTask;
    }

    public void AddKey(PartnerApiKey key) => _keys[key.KeyHash] = key;
    public void AddAccount(PartnerAccount account) => _accounts[account.Id] = account;
}
