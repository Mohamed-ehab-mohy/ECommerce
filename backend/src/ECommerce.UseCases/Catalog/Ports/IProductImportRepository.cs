using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Ports;

public interface IProductImportRepository
{
    Task<ProductImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(ProductImport import);
}
