using NpgsqlTypes;

namespace ECommerce.Infrastructure.Search;

public sealed class ProductSearchDocument
{
    public Guid ProductId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public Guid? BrandId { get; set; }

    public string? Category { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal ListAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public decimal RatingAverage { get; set; }

    public int RatingCount { get; set; }

    public NpgsqlTsVector? SearchVector { get; set; }
}
