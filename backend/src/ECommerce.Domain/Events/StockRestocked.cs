using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record StockRestocked(string Sku, Guid WarehouseId, int Quantity) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
