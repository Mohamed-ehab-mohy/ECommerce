using ECommerce.Domain.Catalog;

namespace ECommerce.UseCases.Catalog.Responses;

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Slug,
    string Name,
    string? Description,
    string Currency,
    decimal ListAmount,
    decimal? OfferAmount,
    ProductStatus Status,
    bool IsFeatured,
    Guid? CategoryId,
    Guid? BrandId);

public static class ProductResponseFactory
{
    public static ProductResponse From(Product product, string? locale = null, string? currency = null)
    {
        var translation = SelectTranslation(product, locale);
        var price = SelectPrice(product, currency);

        return new ProductResponse(
            product.Id,
            product.Sku,
            product.Slug,
            translation?.Name ?? string.Empty,
            translation?.Description,
            price?.Currency ?? string.Empty,
            price?.ListAmount ?? 0m,
            price?.OfferAmount,
            product.Status,
            product.IsFeatured,
            product.CategoryId,
            product.BrandId);
    }

    private static ProductTranslation? SelectTranslation(Product product, string? locale)
    {
        if (locale is not null)
        {
            var match = product.Translations.FirstOrDefault(item => item.Locale == locale);
            if (match is not null)
            {
                return match;
            }
        }

        return product.Translations.FirstOrDefault();
    }

    private static ProductPrice? SelectPrice(Product product, string? currency)
    {
        if (currency is not null)
        {
            var match = product.Prices.FirstOrDefault(item => item.Currency == currency);
            if (match is not null)
            {
                return match;
            }
        }

        return product.Prices.FirstOrDefault();
    }
}
