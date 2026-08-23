using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Ports;

public interface IProductSearchRepository
{
    Task<ProductSearchPage> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken);
}

public sealed record ProductSearchCriteria(
    string? Query,
    string Locale,
    Guid? CategoryId,
    Guid? BrandId,
    decimal? PriceGte,
    decimal? PriceLte,
    decimal? RatingGte,
    int Page,
    int PageSize);

public sealed record ProductSearchPage(
    IReadOnlyList<Product> Items,
    int TotalCount,
    ProductSearchFacets Facets);

public sealed record ProductSearchFacets(
    IReadOnlyList<ProductFacetBucket> Categories,
    IReadOnlyList<ProductFacetBucket> Brands,
    IReadOnlyList<PriceRangeFacet> PriceRanges,
    IReadOnlyList<RatingFacet> Ratings);

public sealed record ProductFacetBucket(Guid Id, string Name, int Count);

public sealed record PriceRangeFacet(string Key, string Label, decimal? Min, decimal? Max, int Count);

public sealed record RatingFacet(int Stars, int Count);
