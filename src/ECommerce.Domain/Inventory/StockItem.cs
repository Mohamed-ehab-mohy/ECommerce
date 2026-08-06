using ECommerce.Domain.Common;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Inventory;

public sealed class StockItem : BaseEntity<Guid>
{
    private StockItem()
    {
        Sku = string.Empty;
    }

    public string Sku { get; private set; }

    public Guid WarehouseId { get; private set; }

    public int OnHand { get; private set; }

    public int Allocated { get; private set; }

    public int Available => OnHand - Allocated;

    public static StockItem Create(string sku, Guid warehouseId, DateTime utcNow)
    {
        return new StockItem
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim().ToUpperInvariant(),
            WarehouseId = warehouseId,
            OnHand = 0,
            Allocated = 0,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Apply(StockMovement movement, DateTime utcNow)
    {
        var nextOnHand = OnHand + movement.OnHandDelta;
        var nextAllocated = Allocated + movement.AllocatedDelta;
        var nextAvailable = nextOnHand - nextAllocated;

        if (nextOnHand < 0)
        {
            throw new StockBalanceException(StockErrors.InsufficientOnHand);
        }

        if (nextAllocated < 0)
        {
            throw new StockBalanceException(StockErrors.InsufficientAllocated);
        }

        if (nextAvailable < 0)
        {
            throw new StockBalanceException(StockErrors.InsufficientAvailable);
        }

        OnHand = nextOnHand;
        Allocated = nextAllocated;
        UpdatedAt = utcNow;
    }
}
