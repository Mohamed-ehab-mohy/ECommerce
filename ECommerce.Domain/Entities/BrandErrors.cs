namespace ECommerce.Domain.Entities;

public static class BrandErrors
{
    public static readonly Error BrandNotFound = new("Brand.NotFound", "Brand not found.", ErrorType.NotFound);
    public static readonly Error InvalidName = new("Brand.InvalidName", "Brand name cannot be empty.", ErrorType.Validation);
    public static readonly Error NameTooLong = new("Brand.NameTooLong", $"Brand name cannot exceed {ProductBrand.MaxNameLength} characters.", ErrorType.Validation);
    public static readonly Error DuplicateName = new("Brand.DuplicateName", "A brand with this name already exists.", ErrorType.Conflict);
}
