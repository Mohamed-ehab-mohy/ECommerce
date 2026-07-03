using ECommerce.UseCases.Products.DTOs;

namespace ECommerce.UseCases.Products;

public interface ITypeQueryService
{
    Task<IReadOnlyList<GetAllTypesResponse>> GetAllAsync(CancellationToken ct = default);
}
