using ECommerce.Domain.Catalog;
using ECommerce.Domain.Events;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Common;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Search;

public sealed class ProductSearchIndexSynchronizer(ECommerceDbContext dbContext)
    : IEventHandler<ProductCreated>,
      IEventHandler<ProductUpdated>,
      IEventHandler<ProductDeactivated>
{
    public Task HandleAsync(ProductCreated domainEvent, CancellationToken cancellationToken) =>
        UpsertAsync(domainEvent.ProductId, cancellationToken);

    public Task HandleAsync(ProductUpdated domainEvent, CancellationToken cancellationToken) =>
        UpsertAsync(domainEvent.ProductId, cancellationToken);

    public Task HandleAsync(ProductDeactivated domainEvent, CancellationToken cancellationToken) =>
        RemoveAsync(domainEvent.ProductId, cancellationToken);

    public async Task UpsertAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Set<Product>()
            .Include(product => product.Translations)
            .Include(product => product.Prices)
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);

        if (product is null)
        {
            return;
        }

        var defaultPrice = product.Prices.FirstOrDefault();
        if (defaultPrice is null)
        {
            return;
        }

        var brand = product.BrandId is null
            ? null
            : await dbContext.Set<Brand>()
                .AsNoTracking()
                .FirstOrDefaultAsync(brand => brand.Id == product.BrandId, cancellationToken);

        var category = product.CategoryId is null
            ? null
            : await dbContext.Set<Category>()
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.Id == product.CategoryId, cancellationToken);

        if (product.Translations.Count == 0)
        {
            await RemoveAsync(productId, cancellationToken);
            return;
        }

        var documents = product.Translations.Select(translation => new ProductSearchDocument
        {
            ProductId = product.Id,
            Locale = translation.Locale,
            Name = translation.Name,
            Description = translation.Description,
            Sku = product.Sku,
            Brand = brand?.Name,
            BrandId = product.BrandId,
            Category = category?.Name,
            CategoryId = product.CategoryId,
            ListAmount = defaultPrice.ListAmount,
            Currency = defaultPrice.Currency
        }).ToList();

        var existing = await dbContext.Set<ProductSearchDocument>()
            .Where(document => document.ProductId == productId)
            .ToListAsync(cancellationToken);

        dbContext.Set<ProductSearchDocument>().RemoveRange(existing);
        dbContext.Set<ProductSearchDocument>().AddRange(documents);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid productId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<ProductSearchDocument>()
            .Where(document => document.ProductId == productId)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        dbContext.Set<ProductSearchDocument>().RemoveRange(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
