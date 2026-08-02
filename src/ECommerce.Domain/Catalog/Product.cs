using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Catalog;

public sealed class Product : BaseEntity<Guid>
{
    private readonly List<ProductTranslation> _translations = [];

    private readonly List<ProductPrice> _prices = [];

    private Product()
    {
        Sku = string.Empty;
        Slug = string.Empty;
        ImageUrls = [];
        Attributes = new Dictionary<string, string>();
    }

    public string Sku { get; private set; }

    public string Slug { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Guid? BrandId { get; private set; }

    public ProductStatus Status { get; private set; }

    public bool IsFeatured { get; private set; }

    public List<string> ImageUrls { get; private set; }

    public Dictionary<string, string> Attributes { get; private set; }

    public IReadOnlyCollection<ProductTranslation> Translations => _translations;

    public IReadOnlyCollection<ProductPrice> Prices => _prices;

    public static Product Create(
        string sku,
        string slug,
        string locale,
        string name,
        string? description,
        string currency,
        decimal listAmount,
        decimal? offerAmount,
        Guid? categoryId,
        Guid? brandId,
        bool isFeatured,
        ProductStatus status,
        DateTime utcNow)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Slug = slug,
            CategoryId = categoryId,
            BrandId = brandId,
            Status = status,
            IsFeatured = isFeatured,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        product.SetTranslation(locale, name, description);
        product.SetPrice(currency, listAmount, offerAmount, utcNow);

        product.AddDomainEvent(new ProductCreated(
            product.Id,
            product.Sku,
            product.Slug,
            name,
            currency,
            listAmount,
            offerAmount));

        return product;
    }

    public void UpdateDetails(
        string? slug,
        Guid? categoryId,
        Guid? brandId,
        bool? isFeatured,
        ProductStatus? status,
        string? locale,
        string? name,
        string? description,
        string? currency,
        decimal? listAmount,
        decimal? offerAmount,
        DateTime utcNow)
    {
        if (!string.IsNullOrWhiteSpace(slug))
        {
            Slug = slug;
        }

        if (categoryId is not null)
        {
            CategoryId = categoryId;
        }

        if (brandId is not null)
        {
            BrandId = brandId;
        }

        if (isFeatured is not null)
        {
            IsFeatured = isFeatured.Value;
        }

        if (status is not null)
        {
            Status = status.Value;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            SetTranslation(locale ?? "en", name, description);
        }

        if (currency is not null && listAmount is not null)
        {
            SetPrice(currency, listAmount.Value, offerAmount, utcNow);
        }

        UpdatedAt = utcNow;

        AddDomainEvent(new ProductUpdated(
            Id,
            Sku,
            Slug,
            GetDefaultName(),
            GetDefaultCurrency(),
            GetDefaultListAmount(),
            GetDefaultOfferAmount()));
    }

    public void Activate()
    {
        Status = ProductStatus.Active;
        IsDeleted = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductDeactivated(Id, Sku, Slug));
    }

    private void SetTranslation(string locale, string name, string? description)
    {
        var translation = _translations.FirstOrDefault(item => item.Locale == locale);
        if (translation is null)
        {
            _translations.Add(ProductTranslation.Create(Id, locale, name, description, null, null));
        }
        else
        {
            translation.Update(name, description);
        }
    }

    private void SetPrice(string currency, decimal listAmount, decimal? offerAmount, DateTime utcNow)
    {
        var price = _prices.FirstOrDefault(item => item.Currency == currency);
        if (price is null)
        {
            _prices.Add(ProductPrice.Create(Id, currency, listAmount, offerAmount, utcNow));
        }
        else
        {
            price.Update(listAmount, offerAmount, utcNow);
        }
    }

    private string GetDefaultName() => _translations.FirstOrDefault()?.Name ?? string.Empty;

    private string GetDefaultCurrency() => _prices.FirstOrDefault()?.Currency ?? string.Empty;

    private decimal GetDefaultListAmount() => _prices.FirstOrDefault()?.ListAmount ?? 0m;

    private decimal? GetDefaultOfferAmount() => _prices.FirstOrDefault()?.OfferAmount;
}
