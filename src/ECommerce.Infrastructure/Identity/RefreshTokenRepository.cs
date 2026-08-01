using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Identity;

public sealed class RefreshTokenRepository(ECommerceDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task<int> RevokeFamilyAsync(Guid familyId, DateTime utcNow, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, utcNow)
                    .SetProperty(token => token.UpdatedAt, utcNow),
                cancellationToken);

    public Task<int> RevokeAllByUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, utcNow)
                    .SetProperty(token => token.UpdatedAt, utcNow),
                cancellationToken);

    public Task<int> TryRevokeAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Where(token => token.Id == id && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, utcNow)
                    .SetProperty(token => token.UpdatedAt, utcNow),
                cancellationToken);

    public void Add(RefreshToken token) => dbContext.RefreshTokens.Add(token);
}
