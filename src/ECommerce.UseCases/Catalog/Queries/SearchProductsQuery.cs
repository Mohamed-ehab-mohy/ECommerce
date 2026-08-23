using ECommerce.UseCases.Catalog.Responses;

namespace ECommerce.UseCases.Catalog.Queries;

public sealed record SearchProductsQuery(
    string? Q,
    Guid? CategoryId,
    Guid? BrandId,
    decimal? PriceGte,
    decimal? PriceLte,
    decimal? RatingGte,
    int Page = 1,
    int PageSize = 20,
    string? Locale = null,
    string? Currency = null) : IRequest<Result<SearchProductsResponse>>;
