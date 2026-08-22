using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Data;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Search;

public sealed class ProductIndexerService(
    ElasticsearchClient client,
    ECommerceDbContext dbContext,
    IOptions<ElasticSearchOptions> options,
    ILogger<ProductIndexerService> logger)
{
    public async Task IndexAllProductsAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return;

        var products = await dbContext.Products
            .Where(p => p.Status == ProductStatus.Active && !p.IsDeleted)
            .Include(p => p.Translations)
            .Include(p => p.Prices)
            .ToListAsync(cancellationToken);

        if (products.Count == 0) return;

        var documents = products.Select(MapToDocument).ToList();

        var response = await client.BulkAsync(b => b
            .Index(options.Value.IndexName)
            .IndexMany(documents), cancellationToken);

        if (response.Errors)
        {
            foreach (var item in response.ItemsWithErrors)
            {
                logger.LogError("Failed to index product {ProductId}: {Error}", item.Id, item.Error?.Reason);
            }
        }
        else
        {
            logger.LogInformation("Indexed {Count} products into Elasticsearch", documents.Count);
        }
    }

    public async Task IndexProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return;

        var document = MapToDocument(product);
        await client.IndexAsync(document, i => i.Index(options.Value.IndexName), cancellationToken);
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return;

        await client.DeleteAsync<ElasticProductDocument>(productId, d => d.Index(options.Value.IndexName), cancellationToken);
    }

    private static ElasticProductDocument MapToDocument(Product product)
    {
        var translation = product.Translations.FirstOrDefault();
        var price = product.Prices.FirstOrDefault();
        return new ElasticProductDocument
        {
            Id = product.Id,
            Sku = product.Sku,
            Slug = product.Slug,
            Name = translation?.Name ?? string.Empty,
            Description = translation?.Description,
            Locale = translation?.Locale ?? "en",
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            Currency = price?.Currency ?? string.Empty,
            ListAmount = price?.ListAmount ?? 0m,
            OfferAmount = price?.OfferAmount,
            IsFeatured = product.IsFeatured,
            Status = product.Status.ToString(),
            ImageUrls = product.ImageUrls,
            Attributes = product.Attributes,
            CreatedAt = product.CreatedAt
        };
    }
}
