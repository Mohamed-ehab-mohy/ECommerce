using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Catalog;

public static class ProductImportErrors
{
    public static readonly Error ImportNotFound = new(
        "Catalog.ImportNotFound",
        "The product import was not found.",
        ErrorType.NotFound);
}
