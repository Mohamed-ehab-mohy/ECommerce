namespace ECommerce.UseCases.Catalog.Responses;

public sealed record SearchProductsResponse(
    IReadOnlyList<ProductResponse> Items,
    SearchFacetsResponse Facets,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNext);

public sealed record SearchFacetsResponse(
    IReadOnlyList<SearchFacetItem> Categories,
    IReadOnlyList<SearchFacetItem> Brands,
    IReadOnlyList<SearchPriceRange> PriceRanges,
    IReadOnlyList<SearchRatingFacet> Ratings);

public sealed record SearchFacetItem(Guid Id, string Name, int Count);

public sealed record SearchPriceRange(string Key, string Label, decimal? Min, decimal? Max, int Count);

public sealed record SearchRatingFacet(int Stars, int Count);
