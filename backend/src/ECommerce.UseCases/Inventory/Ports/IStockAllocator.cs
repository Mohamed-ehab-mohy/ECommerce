
namespace ECommerce.UseCases.Inventory.Ports;

public sealed record AllocationRequestItem(string Sku, int Quantity);

public sealed record StockShortfall(string Sku, int Requested, int Available);

public sealed record StockAllocationLine(Guid StockItemId, string Sku, Guid WarehouseId, int Quantity);

public sealed record StockAllocationResult(
    IReadOnlyList<StockAllocationLine> Allocated,
    IReadOnlyList<StockShortfall> Shortfalls)
{
    public bool HasShortfalls => Shortfalls.Count > 0;
}

public interface IStockAllocator
{
    Task<StockAllocationResult> AllocateAsync(
        IReadOnlyCollection<AllocationRequestItem> items,
        string reason,
        string reference,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<StockReleaseResult> ReleaseAsync(
        IReadOnlyCollection<AllocationRequestItem> items,
        string reason,
        string reference,
        DateTime utcNow,
        CancellationToken cancellationToken);
}

public sealed record StockReleaseResult(IReadOnlyList<StockReleaseLine> Released)
{
    public bool HasLines => Released.Count > 0;
}

public sealed record StockReleaseLine(Guid StockItemId, string Sku, Guid WarehouseId, int Quantity);
