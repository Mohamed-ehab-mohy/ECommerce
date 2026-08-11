using ECommerce.Domain.Pricing;

namespace ECommerce.UseCases.Promotions.Ports;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Promotion>> GetActiveForScopeAsync(DateTime utcNow, CancellationToken cancellationToken);

    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken);

    void Add(Promotion promotion);
}
