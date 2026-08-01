namespace ECommerce.Domain.Audit;

public static class AuditActions
{
    public const string Login = "identity.login";
    public const string ProfileUpdated = "identity.profile.updated";
    public const string AddressAdded = "identity.address.added";
    public const string AddressRemoved = "identity.address.removed";
}
