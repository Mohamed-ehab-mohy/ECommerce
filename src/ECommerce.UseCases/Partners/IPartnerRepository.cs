using ECommerce.Domain.Partners;

namespace ECommerce.UseCases.Partners;

public interface IPartnerRepository
{
    Task<PartnerApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken);
    Task<PartnerApiKey?> GetApiKeyByIdAsync(Guid apiKeyId, CancellationToken cancellationToken);
    Task<PartnerAccount?> GetByIdAsync(Guid partnerId, CancellationToken cancellationToken);
    Task RecordUsageAsync(Guid apiKeyId, DateTime utcNow, CancellationToken cancellationToken);
    Task CreateAccountAsync(PartnerAccount account, CancellationToken cancellationToken);
    Task CreateApiKeyAsync(PartnerApiKey apiKey, CancellationToken cancellationToken);
    Task UpdateApiKeyAsync(PartnerApiKey apiKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartnerApiKey>> ListApiKeysByPartnerAsync(Guid partnerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartnerAccount>> ListAccountsAsync(CancellationToken cancellationToken);
}
