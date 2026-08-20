namespace ECommerce.UseCases.Partners;

public interface IPartnerRepository
{
    Task<PartnerApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken);
    Task<PartnerAccount?> GetByIdAsync(Guid partnerId, CancellationToken cancellationToken);
    Task RecordUsageAsync(Guid apiKeyId, DateTime utcNow, CancellationToken cancellationToken);
}
