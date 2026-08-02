namespace ECommerce.Domain.Audit;

public static class AuditActions
{
    public const string Login = "identity.login";
    public const string ProfileUpdated = "identity.profile.updated";
    public const string AddressAdded = "identity.address.added";
    public const string AddressRemoved = "identity.address.removed";
    public const string ProductCreated = "catalog.product.created";
    public const string ProductUpdated = "catalog.product.updated";
    public const string ProductDeactivated = "catalog.product.deactivated";
    public const string CategoryCreated = "catalog.category.created";
    public const string CategoryUpdated = "catalog.category.updated";
    public const string BrandCreated = "catalog.brand.created";
    public const string BrandUpdated = "catalog.brand.updated";
}
