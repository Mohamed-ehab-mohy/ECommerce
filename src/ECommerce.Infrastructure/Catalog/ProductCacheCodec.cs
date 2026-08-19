using System.Text.Json;
using ECommerce.Domain.Catalog;

namespace ECommerce.Infrastructure.Catalog;

internal static class ProductCacheCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(Product product)
    {
        var dto = ToDto(product);
        return JsonSerializer.Serialize(dto, Options);
    }

    public static Product Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<ProductCacheDto>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize cached product.");

        return ToProduct(dto);
    }

    public static string SerializeList(IReadOnlyList<Product> products)
    {
        var dtos = products.Select(ToDto).ToList();
        return JsonSerializer.Serialize(dtos, Options);
    }

    public static IReadOnlyList<Product> DeserializeList(string json)
    {
        var dtos = JsonSerializer.Deserialize<List<ProductCacheDto>>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize cached product list.");

        return dtos.Select(ToProduct).ToList();
    }

    private static ProductCacheDto ToDto(Product product) => new(
        product.Id,
        product.Sku,
        product.Slug,
        product.CategoryId,
        product.BrandId,
        product.Status.ToString(),
        product.IsFeatured,
        product.Backorderable,
        product.ImageUrls,
        product.Attributes,
        product.CreatedAt,
        product.UpdatedAt,
        product.Translations
            .Select(t => new ProductTranslationCacheDto(
                t.Locale, t.Name, t.Description, t.MetaTitle, t.MetaDescription))
            .ToList(),
        product.Prices
            .Select(p => new ProductPriceCacheDto(
                p.Currency, p.ListAmount, p.OfferAmount))
            .ToList());

    private static Product ToProduct(ProductCacheDto dto) => Product.Rehydrate(
        dto.Id,
        dto.Sku,
        dto.Slug,
        dto.CategoryId,
        dto.BrandId,
        Enum.Parse<ProductStatus>(dto.Status),
        dto.IsFeatured,
        dto.Backorderable,
        dto.ImageUrls,
        dto.Attributes,
        dto.CreatedAt,
        dto.UpdatedAt,
        dto.Translations.Select(t => new ProductTranslationCache(t.Locale, t.Name, t.Description, t.MetaTitle, t.MetaDescription)),
        dto.Prices.Select(p => new ProductPriceCache(p.Currency, p.ListAmount, p.OfferAmount)));

    private sealed record ProductTranslationCacheDto(
        string Locale,
        string Name,
        string? Description,
        string? MetaTitle,
        string? MetaDescription);

    private sealed record ProductPriceCacheDto(
        string Currency,
        decimal ListAmount,
        decimal? OfferAmount);

    private sealed record ProductCacheDto(
        Guid Id,
        string Sku,
        string Slug,
        Guid? CategoryId,
        Guid? BrandId,
        string Status,
        bool IsFeatured,
        bool Backorderable,
        List<string> ImageUrls,
        Dictionary<string, string> Attributes,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        List<ProductTranslationCacheDto> Translations,
        List<ProductPriceCacheDto> Prices);
}
