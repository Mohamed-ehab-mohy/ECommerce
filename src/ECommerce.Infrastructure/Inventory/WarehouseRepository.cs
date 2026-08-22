using ECommerce.Domain.Inventory;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.Infrastructure.Inventory;

public sealed class WarehouseRepository(ECommerceDbContext dbContext) : IWarehouseRepository
{
    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Warehouse>().SingleOrDefaultAsync(warehouse => warehouse.Id == id && !warehouse.IsDeleted, cancellationToken);

    public Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Set<Warehouse>().SingleOrDefaultAsync(warehouse => warehouse.Code == code && !warehouse.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<Warehouse>()
            .Where(warehouse => !warehouse.IsDeleted)
            .OrderBy(warehouse => warehouse.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        dbContext.Set<Warehouse>().CountAsync(warehouse => !warehouse.IsDeleted, cancellationToken);

    public void Add(Warehouse warehouse) => dbContext.Set<Warehouse>().Add(warehouse);
}
