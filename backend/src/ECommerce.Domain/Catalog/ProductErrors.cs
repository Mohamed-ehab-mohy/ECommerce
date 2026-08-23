
namespace ECommerce.Domain.Catalog;

public static class ProductErrors
{
    public static readonly Error ProductNotFound = new(
        "Product.ProductNotFound",
        "The product was not found.",
        ErrorType.NotFound);

    public static readonly Error SkuAlreadyExists = new(
        "Product.SkuAlreadyExists",
        "A product with this SKU already exists.",
        ErrorType.Conflict);

    public static readonly Error SlugAlreadyExists = new(
        "Product.SlugAlreadyExists",
        "A product with this slug already exists.",
        ErrorType.Conflict);
}
