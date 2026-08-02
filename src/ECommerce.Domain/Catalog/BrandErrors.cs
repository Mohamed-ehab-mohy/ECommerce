using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Catalog;

public static class BrandErrors
{
    public static readonly Error BrandNotFound = new(
        "Brand.BrandNotFound",
        "The brand was not found.",
        ErrorType.NotFound);

    public static readonly Error NameAlreadyExists = new(
        "Brand.NameAlreadyExists",
        "A brand with this name already exists.",
        ErrorType.Conflict);
}
