using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Catalog.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Catalog;

public sealed class ProductRepository(ECommerceDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        CompiledQueries.GetProductById(dbContext, id);

    public Task<Product?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken) =>
        CompiledQueries.GetActiveProductById(dbContext, id);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var items = await dbContext.Set<Product>()
            .Include(product => product.Translations)
            .Include(product => product.Prices)
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyList<Product>> GetBySkusAsync(
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken)
    {
        if (skus.Count == 0)
        {
            return [];
        }

        var items = await dbContext.Set<Product>()
            .Where(product => skus.Contains(product.Sku))
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        dbContext.Set<Product>().AnyAsync(product => product.Sku == sku, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid excludeProductId, CancellationToken cancellationToken) =>
        dbContext.Set<Product>().AnyAsync(
            product => product.Slug == slug && product.Id != excludeProductId,
            cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Product>().AnyAsync(product => product.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<Product>()
            .Include(product => product.Translations)
            .Include(product => product.Prices)
            .Where(product => product.Status == ProductStatus.Active && !product.IsDeleted)
            .OrderBy(product => product.Slug)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        dbContext.Set<Product>().CountAsync(
            product => product.Status == ProductStatus.Active && !product.IsDeleted,
            cancellationToken);

    public void Add(Product product) => dbContext.Set<Product>().Add(product);
}
