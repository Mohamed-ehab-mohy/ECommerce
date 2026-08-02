using ECommerce.Domain.Common;

namespace ECommerce.Domain.Catalog;

public sealed class Product : BaseEntity<Guid>
{
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

    public static Product Create(
        string sku,
        string slug,
        Guid? categoryId,
        Guid? brandId,
        bool isFeatured,
        DateTime utcNow)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Slug = slug,
            CategoryId = categoryId,
            BrandId = brandId,
            Status = ProductStatus.Draft,
            IsFeatured = isFeatured,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Activate()
    {
        Status = ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }
}
