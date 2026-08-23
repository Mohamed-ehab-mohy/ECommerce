using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Responses;

public sealed record BrandResponse(Guid Id, string Name, string? Description, string? Website);

public sealed record PagedBrandsResponse(
    IReadOnlyList<BrandResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
