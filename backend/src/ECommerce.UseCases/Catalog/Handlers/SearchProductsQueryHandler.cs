using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Catalog.Queries;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Pricing;

namespace ECommerce.UseCases.Catalog.Handlers;

public sealed class SearchProductsQueryHandler(
    IProductSearchRepository search,
    ILocaleCatalog locales,
    ICurrencyCatalog currencies,
    IValidator<SearchProductsQuery> validator) : IRequestHandler<SearchProductsQuery, Result<SearchProductsResponse>>
{
    public async Task<Result<SearchProductsResponse>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<SearchProductsResponse>();
        }

        var locale = locales.IsSupported(request.Locale)
            ? request.Locale!.Trim().ToLowerInvariant()
            : locales.DefaultLocale;

        var criteria = new ProductSearchCriteria(
            string.IsNullOrWhiteSpace(request.Q) ? null : request.Q.Trim(),
            locale,
            request.CategoryId,
            request.BrandId,
            request.PriceGte,
            request.PriceLte,
            request.RatingGte,
            request.Page,
            request.PageSize);

        var page = await search.SearchAsync(criteria, cancellationToken);

        return Result<SearchProductsResponse>.Success(new SearchProductsResponse(
            page.Items
                .Select(product => ProductResponseFactory.From(product, locales, currencies, request.Locale, request.Currency))
                .ToList(),
            MapFacets(page.Facets),
            request.Page,
            request.PageSize,
            page.TotalCount,
            request.Page * request.PageSize < page.TotalCount));
    }

    private static SearchFacetsResponse MapFacets(ProductSearchFacets facets) =>
        new(
            facets.Categories.Select(item => new SearchFacetItem(item.Id, item.Name, item.Count)).ToList(),
            facets.Brands.Select(item => new SearchFacetItem(item.Id, item.Name, item.Count)).ToList(),
            facets.PriceRanges
                .Select(item => new SearchPriceRange(item.Key, item.Label, item.Min, item.Max, item.Count))
                .ToList(),
            facets.Ratings.Select(item => new SearchRatingFacet(item.Stars, item.Count)).ToList());
}
