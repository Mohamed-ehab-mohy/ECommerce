namespace ECommerce.UseCases.Products.DTOs;

public sealed record GetAllProductsResponse(
    Guid Id,
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    string ProductType,
    string ProductBrand);
