using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Catalog.Ports;

namespace ECommerce.Infrastructure.Catalog;

public sealed class BrandRepository(ECommerceDbContext dbContext) : IBrandRepository
{
    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Brand>().SingleOrDefaultAsync(brand => brand.Id == id, cancellationToken);

    public Task<Brand?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Set<Brand>().SingleOrDefaultAsync(brand => brand.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Brand>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<Brand>()
            .Where(brand => !brand.IsDeleted)
            .OrderBy(brand => brand.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        dbContext.Set<Brand>().CountAsync(brand => !brand.IsDeleted, cancellationToken);

    public void Add(Brand brand) => dbContext.Set<Brand>().Add(brand);
}
