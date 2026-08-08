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

    public long Version { get; private set; }

    public int LowStockThreshold { get; private set; }

    public DateTime? LowStockNotifiedAt { get; private set; }

    public TimeSpan LowStockCooldown { get; private set; } = TimeSpan.FromHours(24);

    public static StockItem Create(
        string sku,
        Guid warehouseId,
        DateTime utcNow,
        int lowStockThreshold = 0)
    {
        return new StockItem
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim().ToUpperInvariant(),
            WarehouseId = warehouseId,
            OnHand = 0,
            Allocated = 0,
            Version = 1,
            LowStockThreshold = lowStockThreshold,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void SetVersion(long version) => Version = version;

    public void SetLowStockThreshold(int threshold, DateTime utcNow)
    {
        if (threshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "A low-stock threshold must not be negative.");
        }

        LowStockThreshold = threshold;
        UpdatedAt = utcNow;
    }

    public void SetLowStockCooldown(TimeSpan cooldown) => LowStockCooldown = cooldown;

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

        EvaluateLowStock(utcNow);
    }

    private void EvaluateLowStock(DateTime utcNow)
    {
        if (LowStockThreshold <= 0 || Available > LowStockThreshold)
        {
            return;
        }

        if (LowStockNotifiedAt is { } notifiedAt && utcNow - notifiedAt < LowStockCooldown)
        {
            return;
        }

        LowStockNotifiedAt = utcNow;
        AddDomainEvent(new Events.LowStockAlertRaised(Id, Sku, WarehouseId, Available, LowStockThreshold));
    }
}
