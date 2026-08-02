using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Ports;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Brand>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    void Add(Brand brand);
}
