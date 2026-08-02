namespace ECommerce.Domain.Catalog;

public sealed class ProductTranslation
{
    private ProductTranslation()
    {
        Locale = string.Empty;
        Name = string.Empty;
    }

    public Guid ProductId { get; private set; }

    public string Locale { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public static ProductTranslation Create(
        Guid productId,
        string locale,
        string name,
        string? description,
        string? metaTitle,
        string? metaDescription)
    {
        return new ProductTranslation
        {
            ProductId = productId,
            Locale = locale,
            Name = name,
            Description = description,
            MetaTitle = metaTitle,
            MetaDescription = metaDescription
        };
    }
}
