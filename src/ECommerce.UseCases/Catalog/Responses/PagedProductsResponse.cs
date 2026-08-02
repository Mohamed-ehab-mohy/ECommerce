using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Responses;

public sealed record PagedProductsResponse(
    IReadOnlyList<ProductResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
