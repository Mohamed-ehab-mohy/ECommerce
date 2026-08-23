using ECommerce.Domain.Identity;

namespace ECommerce.UseCases.Identity.Ports;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<int> RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken);

    Task<int> RevokeAllByUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken);

    Task<int> TryRevokeAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken);

    void Add(RefreshToken token);
}
