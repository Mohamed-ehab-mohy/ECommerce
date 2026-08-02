using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Catalog.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Catalog;

public sealed class CategoryRepository(ECommerceDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Category>().SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Category>().SingleOrDefaultAsync(category => category.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.Set<Category>()
            .Where(category => !category.IsDeleted)
            .ToListAsync(cancellationToken);

        return items;
    }

    public void Add(Category category) => dbContext.Set<Category>().Add(category);
}
