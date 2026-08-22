using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Catalog.Ports;
using NpgsqlTypes;

namespace ECommerce.Infrastructure.Search;

public sealed class ProductSearchRepository(ECommerceDbContext dbContext) : IProductSearchRepository
{
    private const double TrigramThreshold = 0.3;

    private static readonly (string Key, string Label, decimal? Min, decimal? Max)[] PriceBuckets =
    [
        ("under-50", "Under 50", null, 50m),
        ("50-100", "50 - 100", 50m, 100m),
        ("100-250", "100 - 250", 100m, 250m),
        ("250-500", "250 - 500", 250m, 500m),
        ("over-500", "Over 500", 500m, null)
    ];

    public async Task<ProductSearchPage> SearchAsync(
        ProductSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var documents = dbContext.Set<ProductSearchDocument>().AsNoTracking();

        var activeProducts = dbContext.Set<Product>()
            .Where(product => product.Status == ProductStatus.Active && !product.IsDeleted);

        var query = from document in documents
                    join product in activeProducts on document.ProductId equals product.Id
                    where document.Locale == criteria.Locale
                    select new { document, product };

        if (criteria.CategoryId.HasValue)
        {
            query = query.Where(item => item.document.CategoryId == criteria.CategoryId);
        }

        if (criteria.BrandId.HasValue)
        {
            query = query.Where(item => item.document.BrandId == criteria.BrandId);
        }

        if (criteria.PriceGte.HasValue)
        {
            query = query.Where(item => item.document.ListAmount >= criteria.PriceGte.Value);
        }

        if (criteria.PriceLte.HasValue)
        {
            query = query.Where(item => item.document.ListAmount <= criteria.PriceLte.Value);
        }

        if (criteria.RatingGte.HasValue)
        {
            query = query.Where(item => item.document.RatingAverage >= criteria.RatingGte.Value);
        }

        var hasQuery = !string.IsNullOrWhiteSpace(criteria.Query);

        if (hasQuery)
        {
            query = query.Where(item =>
                item.document.SearchVector!.Matches(
                    EF.Functions.WebSearchToTsQuery("simple", criteria.Query!)) ||
                EF.Functions.TrigramsSimilarity(item.document.Name, criteria.Query!) >= TrigramThreshold);
        }

        var total = await query.CountAsync(cancellationToken);

        IQueryable<Product> ordered = hasQuery
            ? query
                .OrderByDescending(item =>
                    0.7 * item.document.SearchVector!.RankCoverDensity(
                        EF.Functions.WebSearchToTsQuery("simple", criteria.Query!))
                    + 0.3 * EF.Functions.TrigramsSimilarity(item.document.Name, criteria.Query!))
                .ThenBy(item => item.document.ProductId)
                .Select(item => item.product)
            : query
                .OrderByDescending(item => item.product.CreatedAt)
                .ThenBy(item => item.document.ProductId)
                .Select(item => item.product);

        var page = ordered
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize);

        var itemIds = await page.Select(product => product.Id).ToListAsync(cancellationToken);

        var items = await dbContext.Set<Product>()
            .Include(product => product.Translations)
            .Include(product => product.Prices)
            .AsNoTracking()
            .Where(product => itemIds.Contains(product.Id))
            .ToListAsync(cancellationToken);

        var orderedItems = itemIds
            .Select(id => items.First(product => product.Id == id))
            .ToList();

        var facetRows = await query
            .Select(item => new ProductSearchFacetRow(
                item.document.CategoryId,
                item.document.Category,
                item.document.BrandId,
                item.document.Brand,
                item.document.ListAmount,
                item.document.RatingAverage,
                item.document.RatingCount))
            .ToListAsync(cancellationToken);

        return new ProductSearchPage(orderedItems, total, BuildFacets(facetRows));
    }

    private static ProductSearchFacets BuildFacets(IReadOnlyList<ProductSearchFacetRow> rows)
    {
        var categories = rows
            .Where(row => row.CategoryId.HasValue)
            .GroupBy(row => row.CategoryId!.Value)
            .Select(group => new ProductFacetBucket(group.Key, group.First().Category ?? string.Empty, group.Count()))
            .OrderByDescending(bucket => bucket.Count)
            .ToList();

        var brands = rows
            .Where(row => row.BrandId.HasValue)
            .GroupBy(row => row.BrandId!.Value)
            .Select(group => new ProductFacetBucket(group.Key, group.First().Brand ?? string.Empty, group.Count()))
            .OrderByDescending(bucket => bucket.Count)
            .ToList();

        var priceRanges = PriceBuckets.Select(bucket => new PriceRangeFacet(
            bucket.Key,
            bucket.Label,
            bucket.Min,
            bucket.Max,
            rows.Count(row => MatchesBucket(row.ListAmount, bucket.Min, bucket.Max)))).ToList();

        var ratings = Enumerable.Range(1, 5)
            .Select(stars => new RatingFacet(stars, rows.Count(row => row.RatingCount > 0 && row.RatingAverage >= stars)))
            .ToList();

        return new ProductSearchFacets(categories, brands, priceRanges, ratings);
    }

    private static bool MatchesBucket(decimal amount, decimal? min, decimal? max) =>
        (min is null || amount >= min) && (max is null || amount < max);

    private sealed record ProductSearchFacetRow(
        Guid? CategoryId,
        string? Category,
        Guid? BrandId,
        string? Brand,
        decimal ListAmount,
        decimal RatingAverage,
        int RatingCount);
}
