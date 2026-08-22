using System.Text.Json;
using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Catalog.Ports;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Search;

public sealed class ElasticProductSearchRepository(
    ElasticsearchClient client,
    ECommerceDbContext dbContext,
    IOptions<ElasticSearchOptions> searchOptions,
    ProductSearchRepository fallbackRepository) : IProductSearchRepository
{
    private static readonly (string Key, string Label, decimal? Min, decimal? Max)[] PriceBuckets =
    [
        ("under-50", "Under 50", null, 50m),
        ("50-100", "50 - 100", 50m, 100m),
        ("100-250", "100 - 250", 100m, 250m),
        ("250-500", "250 - 500", 250m, 500m),
        ("over-500", "Over 500", 500m, null)
    ];

    public async Task<ProductSearchPage> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken)
    {
        if (!searchOptions.Value.Enabled)
        {
            return await fallbackRepository.SearchAsync(criteria, cancellationToken);
        }

        SearchResponse<ElasticProductDocument> searchResponse;

        try
        {
            var from = (criteria.Page - 1) * criteria.PageSize;
            var json = BuildSearchJson(criteria, from);
            var request = JsonSerializer.Deserialize<SearchRequest<ElasticProductDocument>>(json)
                ?? new SearchRequest<ElasticProductDocument>(searchOptions.Value.IndexName);
            searchResponse = await client.SearchAsync<ElasticProductDocument>(request, cancellationToken);
        }
        catch
        {
            return await fallbackRepository.SearchAsync(criteria, cancellationToken);
        }

        if (!searchResponse.IsValidResponse || searchResponse.Hits is null)
        {
            return await fallbackRepository.SearchAsync(criteria, cancellationToken);
        }

        var totalHits = (int)searchResponse.Total;
        var productIds = searchResponse.Hits
            .Select(h => h.Id)
            .Where(id => id is not null && Guid.TryParse(id, out _))
            .Select(id => Guid.Parse(id!))
            .ToList();

        var items = await LoadProductsAsync(productIds, cancellationToken);
        var orderedItems = productIds
            .Select(id => items.FirstOrDefault(p => p.Id == id))
            .OfType<Product>()
            .ToList();

        var facets = BuildFacets(searchResponse);
        return new ProductSearchPage(orderedItems, totalHits, facets);
    }

    private static string BuildSearchJson(ProductSearchCriteria criteria, int from)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(criteria.Query);

        var mustClauses = new List<object>();
        var filterClauses = new List<object>
        {
            TermClause("status", "Active"),
            TermClause("locale", criteria.Locale)
        };

        if (hasQuery)
        {
            mustClauses.Add(new Dictionary<string, object>
            {
                ["multi_match"] = new Dictionary<string, object>
                {
                    ["fields"] = new[] { "name^3", "description", "sku^2", "brandName", "categoryName" },
                    ["query"] = criteria.Query!
                }
            });
        }
        else
        {
            mustClauses.Add(new Dictionary<string, object> { ["match_all"] = new Dictionary<string, object>() });
        }

        if (criteria.CategoryId.HasValue)
            filterClauses.Add(TermClause("categoryId", criteria.CategoryId.Value.ToString()));

        if (criteria.BrandId.HasValue)
            filterClauses.Add(TermClause("brandId", criteria.BrandId.Value.ToString()));

        if (criteria.PriceGte.HasValue || criteria.PriceLte.HasValue)
        {
            var rangeDict = new Dictionary<string, object>();
            if (criteria.PriceGte.HasValue) rangeDict["gte"] = (double)criteria.PriceGte.Value;
            if (criteria.PriceLte.HasValue) rangeDict["lte"] = (double)criteria.PriceLte.Value;
            filterClauses.Add(new Dictionary<string, object>
            {
                ["range"] = new Dictionary<string, object> { ["listAmount"] = rangeDict }
            });
        }

        if (criteria.RatingGte.HasValue)
        {
            filterClauses.Add(new Dictionary<string, object>
            {
                ["range"] = new Dictionary<string, object>
                {
                    ["rating"] = new Dictionary<string, object> { ["gte"] = (double)criteria.RatingGte.Value }
                }
            });
        }

        var queryBody = new Dictionary<string, object>
        {
            ["bool"] = new Dictionary<string, object>
            {
                ["must"] = mustClauses,
                ["filter"] = filterClauses
            }
        };

        var priceRangeSpecs = PriceBuckets
            .Select(b => new Dictionary<string, double?> { ["from"] = b.Min is decimal d ? (double)d : null, ["to"] = b.Max is decimal d2 ? (double)d2 : null })
            .Cast<object>()
            .ToArray();

        var ratingRangeSpecs = Enumerable.Range(1, 5)
            .Select(stars => new Dictionary<string, double> { ["from"] = stars, ["to"] = stars + 1 })
            .Cast<object>()
            .ToArray();

        var body = new Dictionary<string, object>
        {
            ["query"] = queryBody,
            ["sort"] = new object[] { new Dictionary<string, object> { ["createdAt"] = new Dictionary<string, string> { ["order"] = "desc" } } },
            ["aggs"] = new Dictionary<string, object>
            {
                ["categories"] = new Dictionary<string, object> { ["terms"] = new Dictionary<string, object> { ["field"] = "categoryId", ["size"] = 50 } },
                ["brands"] = new Dictionary<string, object> { ["terms"] = new Dictionary<string, object> { ["field"] = "brandId", ["size"] = 50 } },
                ["price_ranges"] = new Dictionary<string, object> { ["range"] = new Dictionary<string, object> { ["field"] = "listAmount", ["ranges"] = priceRangeSpecs } },
                ["ratings"] = new Dictionary<string, object> { ["range"] = new Dictionary<string, object> { ["field"] = "rating", ["ranges"] = ratingRangeSpecs } }
            },
            ["from"] = from,
            ["size"] = criteria.PageSize
        };

        return JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static Dictionary<string, object> TermClause(string field, string value) =>
        new()
        {
            ["term"] = new Dictionary<string, object> { [field] = value }
        };

    private ProductSearchFacets BuildFacets(SearchResponse<ElasticProductDocument> response)
    {
        var categories = new List<ProductFacetBucket>();
        if (response.Aggregations?.GetStringTerms("categories") is { } categoryTerms)
        {
            categories = categoryTerms.Buckets
                .Select(b => new ProductFacetBucket(
                    Guid.Parse(b.Key.ToString()),
                    b.Key.ToString(),
                    (int)b.DocCount))
                .OrderByDescending(b => b.Count)
                .ToList();
        }

        var brands = new List<ProductFacetBucket>();
        if (response.Aggregations?.GetStringTerms("brands") is { } brandTerms)
        {
            brands = brandTerms.Buckets
                .Select(b => new ProductFacetBucket(
                    Guid.Parse(b.Key.ToString()),
                    b.Key.ToString(),
                    (int)b.DocCount))
                .OrderByDescending(b => b.Count)
                .ToList();
        }

        var priceRanges = new List<PriceRangeFacet>();
        if (response.Aggregations?.GetRange("price_ranges") is { } priceRangeAgg)
        {
            var buckets = priceRangeAgg.Buckets.ToList();
            for (var i = 0; i < buckets.Count && i < PriceBuckets.Length; i++)
            {
                priceRanges.Add(new PriceRangeFacet(
                    PriceBuckets[i].Key,
                    PriceBuckets[i].Label,
                    PriceBuckets[i].Min,
                    PriceBuckets[i].Max,
                    (int)buckets[i].DocCount));
            }
        }

        var ratings = new List<RatingFacet>();
        if (response.Aggregations?.GetRange("ratings") is { } ratingAgg)
        {
            var ratingBuckets = ratingAgg.Buckets.ToList();
            for (var i = 0; i < ratingBuckets.Count && i < 5; i++)
            {
                ratings.Add(new RatingFacet(i + 1, (int)ratingBuckets[i].DocCount));
            }
        }

        return new ProductSearchFacets(categories, brands, priceRanges, ratings);
    }

    private async Task<List<Product>> LoadProductsAsync(List<Guid> productIds, CancellationToken cancellationToken)
    {
        return productIds.Count == 0 ? [] : await dbContext.Set<Product>()
            .Include(p => p.Translations)
            .Include(p => p.Prices)
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }
}
