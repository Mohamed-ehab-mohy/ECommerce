using System.Collections.Concurrent;
using ECommerce.Domain.Partners;
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

    public Task<PartnerApiKey?> GetApiKeyByIdAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        var key = _keys.Values.FirstOrDefault(k => k.Id == apiKeyId);
        return Task.FromResult(key);
    }

    public Task<PartnerAccount?> GetByIdAsync(Guid partnerId, CancellationToken cancellationToken)
    {
        _accounts.TryGetValue(partnerId, out var account);
        return Task.FromResult(account);
    }

    public Task RecordUsageAsync(Guid apiKeyId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (_keys.Values.FirstOrDefault(k => k.Id == apiKeyId) is { } key)
        {
            key.RecordUsage(utcNow);
        }

        return Task.CompletedTask;
    }

    public Task CreateAccountAsync(PartnerAccount account, CancellationToken cancellationToken)
    {
        _accounts[account.Id] = account;
        return Task.CompletedTask;
    }

    public Task CreateApiKeyAsync(PartnerApiKey apiKey, CancellationToken cancellationToken)
    {
        _keys[apiKey.KeyHash] = apiKey;
        return Task.CompletedTask;
    }

    public Task UpdateApiKeyAsync(PartnerApiKey apiKey, CancellationToken cancellationToken)
    {
        _keys[apiKey.KeyHash] = apiKey;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PartnerApiKey>> ListApiKeysByPartnerAsync(Guid partnerId, CancellationToken cancellationToken)
    {
        var keys = _keys.Values.Where(k => k.PartnerId == partnerId).ToList();
        return Task.FromResult<IReadOnlyList<PartnerApiKey>>(keys);
    }

    public Task<IReadOnlyList<PartnerAccount>> ListAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = _accounts.Values.ToList();
        return Task.FromResult<IReadOnlyList<PartnerAccount>>(accounts);
    }
}
