using ECommerce.UseCases.Products.DTOs;

namespace ECommerce.UseCases.Products;

public interface IProductQueryService
{
    Task<IReadOnlyList<GetAllProductsResponse>> GetAllProductsAsync(CancellationToken ct = default);
    Task<GetByIdProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
