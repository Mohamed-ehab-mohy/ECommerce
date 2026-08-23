namespace ECommerce.API.Controllers;

public sealed record CreateProductRequest(
    string Sku,
    string Slug,
    string Name,
    string? Description,
    string Currency,
    decimal ListAmount,
    decimal? OfferAmount,
    Guid? CategoryId,
    Guid? BrandId,
    bool IsFeatured = false,
    string? Status = null,
    string Locale = "en");

public sealed record UpdateProductRequest(
    string? Slug,
    string? Name,
    string? Description,
    string? Currency,
    decimal? ListAmount,
    decimal? OfferAmount,
    Guid? CategoryId,
    Guid? BrandId,
    bool? IsFeatured,
    string? Status,
    string? Locale);
