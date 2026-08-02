using ECommerce.Domain.Common;

namespace ECommerce.Domain.Catalog;

public sealed class ProductVariant : BaseEntity<Guid>
{
    private ProductVariant()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public Dictionary<string, string> Attributes { get; private set; } = [];

    public static ProductVariant Create(
        Guid productId,
        string sku,
        string name,
        DateTime utcNow)
    {
        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = sku,
            Name = name,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }
}
