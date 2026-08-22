using ECommerce.Domain.Partners;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Partners;

namespace ECommerce.Infrastructure.Partners;

public sealed class PostgresPartnerRepository(ECommerceDbContext db) : IPartnerRepository
{
    public async Task<PartnerApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken)
    {
        return await db.PartnerApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);
    }

    public async Task<PartnerApiKey?> GetApiKeyByIdAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        return await db.PartnerApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);
    }

    public async Task<PartnerAccount?> GetByIdAsync(Guid partnerId, CancellationToken cancellationToken)
    {
        return await db.PartnerAccounts
            .FirstOrDefaultAsync(a => a.Id == partnerId, cancellationToken);
    }

    public async Task RecordUsageAsync(Guid apiKeyId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var key = await db.PartnerApiKeys.FindAsync([apiKeyId], cancellationToken);
        if (key is not null)
        {
            key.RecordUsage(utcNow);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CreateAccountAsync(PartnerAccount account, CancellationToken cancellationToken)
    {
        db.PartnerAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateApiKeyAsync(PartnerApiKey apiKey, CancellationToken cancellationToken)
    {
        db.PartnerApiKeys.Add(apiKey);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateApiKeyAsync(PartnerApiKey apiKey, CancellationToken cancellationToken)
    {
        db.PartnerApiKeys.Update(apiKey);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartnerApiKey>> ListApiKeysByPartnerAsync(Guid partnerId, CancellationToken cancellationToken)
    {
        return await db.PartnerApiKeys
            .Where(k => k.PartnerId == partnerId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartnerAccount>> ListAccountsAsync(CancellationToken cancellationToken)
    {
        return await db.PartnerAccounts
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
