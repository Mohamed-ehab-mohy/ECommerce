using ECommerce.Domain.Inventory;

namespace ECommerce.UseCases.Inventory.Ports;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<Warehouse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    void Add(Warehouse warehouse);
}
