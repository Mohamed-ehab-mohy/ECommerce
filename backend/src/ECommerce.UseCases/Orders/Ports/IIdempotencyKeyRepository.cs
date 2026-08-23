using ECommerce.Domain.Orders;

namespace ECommerce.UseCases.Orders.Ports;

public interface IIdempotencyKeyRepository
{
    Task<IdempotencyKey?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the key if absent. Returns null when this call won the insert;
    /// otherwise returns the already-stored key belonging to the winner.
    /// </summary>
    Task<IdempotencyKey?> AddIfAbsentAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken);
}
