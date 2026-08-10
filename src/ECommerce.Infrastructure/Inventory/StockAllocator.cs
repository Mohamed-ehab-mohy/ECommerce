using ECommerce.Domain.Inventory;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Inventory.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Inventory;

public sealed class StockAllocator(
    ECommerceDbContext dbContext,
    IStockRepository stock) : IStockAllocator
{
    public async Task<StockAllocationResult> AllocateAsync(
        IReadOnlyCollection<AllocationRequestItem> items,
        string reason,
        string reference,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var allocated = new List<StockAllocationLine>();
        var shortfalls = new List<StockShortfall>();

        foreach (var item in items)
        {
            var remaining = item.Quantity;

            var candidates = await LockForUpdateAsync(item.Sku, cancellationToken);
            foreach (var candidate in candidates)
            {
                var take = Math.Min(remaining, candidate.Available);
                if (take <= 0)
                {
                    continue;
                }

                var movement = StockMovement.Create(
                    candidate.Id,
                    StockMovementType.Allocate,
                    take,
                    reason,
                    reference,
                    null,
                    utcNow);

                candidate.Apply(movement, utcNow);
                stock.AddMovement(movement);

                allocated.Add(new StockAllocationLine(candidate.Id, candidate.Sku, candidate.WarehouseId, take));
                remaining -= take;
            }

            if (remaining > 0)
            {
                shortfalls.Add(new StockShortfall(item.Sku, item.Quantity, item.Quantity - remaining));
            }
        }

        if (allocated.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new StockAllocationResult(allocated, shortfalls);
    }

    public async Task<StockReleaseResult> ReleaseAsync(
        IReadOnlyCollection<AllocationRequestItem> items,
        string reason,
        string reference,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var released = new List<StockReleaseLine>();

        foreach (var item in items)
        {
            var remaining = item.Quantity;

            var candidates = await LockForUpdateAsync(item.Sku, cancellationToken);
            foreach (var candidate in candidates)
            {
                var release = Math.Min(remaining, candidate.Allocated);
                if (release <= 0)
                {
                    continue;
                }

                var movement = StockMovement.Create(
                    candidate.Id,
                    StockMovementType.Release,
                    release,
                    reason,
                    reference,
                    null,
                    utcNow);

                candidate.Apply(movement, utcNow);
                stock.AddMovement(movement);

                released.Add(new StockReleaseLine(candidate.Id, candidate.Sku, candidate.WarehouseId, release));
                remaining -= release;
            }
        }

        if (released.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new StockReleaseResult(released);
    }

    private async Task<IReadOnlyList<StockItem>> LockForUpdateAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.StockItems
            .FromSqlInterpolated($"""
                SELECT si.* FROM "stock_items" AS si
                JOIN "warehouses" AS w ON w."id" = si."warehouse_id"
                WHERE si."sku" = {sku} AND si."is_deleted" = FALSE AND w."is_deleted" = FALSE
                ORDER BY w."code"
                FOR UPDATE OF si
                """)
            .ToListAsync(cancellationToken);

        return items;
    }
}
