namespace ECommerce.Domain.Entities;

public static class ProductErrors
{
    public static readonly Error ProductNotFound = new("Product.NotFound", "Product not found.", ErrorType.NotFound);
    public static readonly Error InvalidPrice = new("Product.InvalidPrice", "Product price cannot be negative.", ErrorType.Validation);
    public static readonly Error InvalidName = new("Product.InvalidName", "Product name cannot be empty.", ErrorType.Validation);
    public static readonly Error InvalidDescription = new("Product.InvalidDescription", "Product description cannot be empty.", ErrorType.Validation);
    public static readonly Error ProductBrandNotFound = new("Product.BrandNotFound", "Product brand not found.", ErrorType.NotFound);
    public static readonly Error ProductTypeNotFound = new("Product.TypeNotFound", "Product type not found.", ErrorType.NotFound);
}
