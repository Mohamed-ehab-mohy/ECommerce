using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Catalog.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Catalog;

public sealed class ProductImportRepository(ECommerceDbContext dbContext) : IProductImportRepository
{
    public Task<ProductImport?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<ProductImport>()
            .Include(import => import.Errors)
            .SingleOrDefaultAsync(import => import.Id == id, cancellationToken);

    public void Add(ProductImport import) => dbContext.Set<ProductImport>().Add(import);
}
