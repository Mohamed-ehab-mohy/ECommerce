using ECommerce.Domain.Inventory;

namespace ECommerce.UseCases.Inventory.Ports;

public interface IStockRepository
{
    Task<StockItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<StockItem?> GetBySkuAndWarehouseAsync(string sku, Guid warehouseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> ListBySkuAsync(string sku, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockItem>> ListAsync(int page, int pageSize, Guid? warehouseId, CancellationToken cancellationToken);

    Task<int> CountAsync(Guid? warehouseId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockMovement>> ListMovementsAsync(Guid stockItemId, int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountMovementsAsync(Guid stockItemId, CancellationToken cancellationToken);

    void Add(StockItem stockItem);

    void AddMovement(StockMovement movement);
}
