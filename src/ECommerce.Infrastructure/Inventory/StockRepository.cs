using ECommerce.Domain.Inventory;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Inventory.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Inventory;

public sealed class StockRepository(ECommerceDbContext dbContext) : IStockRepository
{
    public Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<StockItem>().SingleOrDefaultAsync(stockItem => stockItem.Id == id && !stockItem.IsDeleted, cancellationToken);

    public Task<StockItem?> GetBySkuAndWarehouseAsync(string sku, Guid warehouseId, CancellationToken cancellationToken) =>
        dbContext.Set<StockItem>().SingleOrDefaultAsync(
            stockItem => stockItem.Sku == sku && stockItem.WarehouseId == warehouseId && !stockItem.IsDeleted,
            cancellationToken);

    public async Task<IReadOnlyList<StockItem>> ListBySkuAsync(string sku, CancellationToken cancellationToken) =>
        await dbContext.Set<StockItem>()
            .Where(stockItem => stockItem.Sku == sku && !stockItem.IsDeleted)
            .OrderBy(stockItem => stockItem.WarehouseId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StockItem>> LockForTransferAsync(
        string sku,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.StockItems
            .FromSqlInterpolated($"""
                SELECT si.* FROM "stock_items" AS si
                WHERE si."sku" = {sku}
                  AND si."warehouse_id" IN ({fromWarehouseId}, {toWarehouseId})
                  AND si."is_deleted" = FALSE
                ORDER BY si."warehouse_id"
                FOR UPDATE OF si
                """)
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyList<StockItem>> ListAsync(int page, int pageSize, Guid? warehouseId, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<StockItem>().Where(stockItem => !stockItem.IsDeleted);

        if (warehouseId is not null)
        {
            query = query.Where(stockItem => stockItem.WarehouseId == warehouseId);
        }

        var items = await query
            .OrderBy(stockItem => stockItem.Sku)
            .ThenBy(stockItem => stockItem.WarehouseId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountAsync(Guid? warehouseId, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<StockItem>().Where(stockItem => !stockItem.IsDeleted);

        if (warehouseId is not null)
        {
            query = query.Where(stockItem => stockItem.WarehouseId == warehouseId);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovement>> ListMovementsAsync(Guid stockItemId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<StockMovement>()
            .Where(movement => movement.StockItemId == stockItemId && !movement.IsDeleted)
            .OrderByDescending(movement => movement.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountMovementsAsync(Guid stockItemId, CancellationToken cancellationToken) =>
        dbContext.Set<StockMovement>().CountAsync(movement => movement.StockItemId == stockItemId && !movement.IsDeleted, cancellationToken);

    public void Add(StockItem stockItem) => dbContext.Set<StockItem>().Add(stockItem);

    public void AddMovement(StockMovement movement) => dbContext.Set<StockMovement>().Add(movement);
}
