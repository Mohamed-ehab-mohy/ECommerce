using ECommerce.UseCases.Products.DTOs;

namespace ECommerce.UseCases.Products;

public interface IBrandQueryService
{
    Task<IReadOnlyList<GetAllBrandsResponse>> GetAllAsync(CancellationToken ct = default);
}
