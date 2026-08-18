namespace ECommerce.Infrastructure.Search;

public sealed class ElasticProductDocument
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Locale { get; init; } = "en";
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public Guid? BrandId { get; init; }
    public string? BrandName { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal ListAmount { get; init; }
    public decimal? OfferAmount { get; init; }
    public decimal? Rating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsFeatured { get; init; }
    public string Status { get; init; } = "Active";
    public List<string> ImageUrls { get; init; } = [];
    public Dictionary<string, string> Attributes { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
