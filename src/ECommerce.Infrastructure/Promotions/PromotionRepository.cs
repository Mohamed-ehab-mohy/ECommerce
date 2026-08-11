using ECommerce.Domain.Pricing;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Promotions.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Promotions;

public sealed class PromotionRepository(ECommerceDbContext dbContext) : IPromotionRepository
{
    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Promotion>().SingleOrDefaultAsync(promotion => promotion.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Promotion>> GetActiveForScopeAsync(
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        await dbContext.Set<Promotion>()
            .AsNoTracking()
            .Where(promotion => promotion.State == PromotionState.Active)
            .Where(promotion => promotion.StartsAt == null || promotion.StartsAt <= utcNow)
            .Where(promotion => promotion.EndsAt == null || promotion.EndsAt >= utcNow)
            .OrderBy(promotion => promotion.CreatedAt)
            .ThenBy(promotion => promotion.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<Promotion>()
            .AsNoTracking()
            .OrderBy(promotion => promotion.CreatedAt)
            .ThenBy(promotion => promotion.Id)
            .ToListAsync(cancellationToken);

    public void Add(Promotion promotion) => dbContext.Set<Promotion>().Add(promotion);
}
