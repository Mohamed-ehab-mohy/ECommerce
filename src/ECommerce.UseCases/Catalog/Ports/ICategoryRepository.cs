using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Ports;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken cancellationToken);

    void Add(Category category);
}
