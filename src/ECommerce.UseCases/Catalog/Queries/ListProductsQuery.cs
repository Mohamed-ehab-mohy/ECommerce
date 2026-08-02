using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Responses;
using MediatR;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Locale = null,
    string? Currency = null) : IRequest<Result<PagedProductsResponse>>;
