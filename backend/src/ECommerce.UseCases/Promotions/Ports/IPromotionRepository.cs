using ECommerce.Domain.Pricing;

namespace ECommerce.UseCases.Promotions.Ports;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Promotion>> GetActiveForScopeAsync(DateTime utcNow, CancellationToken cancellationToken);

    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Draft campaigns whose schedule has started but not ended; the scheduler activates these (US-E-007).</summary>
    Task<IReadOnlyList<Promotion>> GetDueForActivationAsync(DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Active campaigns whose schedule has ended; the scheduler pauses these (US-E-007).</summary>
    Task<IReadOnlyList<Promotion>> GetDueForPauseAsync(DateTime utcNow, CancellationToken cancellationToken);

    void Add(Promotion promotion);
}
