namespace ECommerce.UseCases.Products.DTOs;

public sealed record GetByIdProductResponse(
    Guid Id,
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    string ProductType,
    string ProductBrand,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
