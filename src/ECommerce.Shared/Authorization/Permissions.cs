namespace ECommerce.Shared.Authorization;

public static class Permissions
{
    public const string CatalogProductWrite = "catalog.product.write";
    public const string CatalogProductDelete = "catalog.product.delete";
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";
    public const string RolesPermissionsWrite = "roles.permissions.write";
    public const string CustomersRead = "customers.read";
    public const string AuthImpersonate = "auth.impersonate";
    public const string AuditRead = "audit.read";

    public static IReadOnlyList<string> All { get; } =
    [
        CatalogProductWrite,
        CatalogProductDelete,
        RolesRead,
        RolesWrite,
        RolesPermissionsWrite,
        CustomersRead,
        AuthImpersonate,
        AuditRead
    ];
}
