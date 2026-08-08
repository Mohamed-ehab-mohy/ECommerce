using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Events;

public sealed record LowStockAlertRaised(
    Guid StockItemId,
    string Sku,
    Guid WarehouseId,
    int Available,
    int Threshold) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
