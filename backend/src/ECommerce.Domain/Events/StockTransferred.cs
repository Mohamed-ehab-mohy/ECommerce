using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record StockTransferred(
    Guid SourceStockItemId,
    Guid TargetStockItemId,
    string Sku,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int Quantity) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
