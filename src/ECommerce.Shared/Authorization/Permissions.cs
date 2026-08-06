namespace ECommerce.Shared.Authorization;

public static class Permissions
{
    public const string CatalogProductWrite = "catalog.product.write";
    public const string CatalogProductDelete = "catalog.product.delete";
    public const string InventoryWarehouseRead = "inventory.warehouse.read";
    public const string InventoryWarehouseWrite = "inventory.warehouse.write";
    public const string InventoryWarehouseDelete = "inventory.warehouse.delete";
    public const string InventoryStockRead = "inventory.stock.read";
    public const string InventoryStockWrite = "inventory.stock.write";
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";
    public const string RolesPermissionsWrite = "roles.permissions.write";
    public const string CustomersRead = "customers.read";
    public const string CustomersPiiRead = "customers.pii.read";
    public const string AuthImpersonate = "auth.impersonate";
    public const string AuditRead = "audit.read";

    public static IReadOnlyList<string> All { get; } =
    [
        CatalogProductWrite,
        CatalogProductDelete,
        InventoryWarehouseRead,
        InventoryWarehouseWrite,
        InventoryWarehouseDelete,
        InventoryStockRead,
        InventoryStockWrite,
        RolesRead,
        RolesWrite,
        RolesPermissionsWrite,
        CustomersRead,
        CustomersPiiRead,
        AuthImpersonate,
        AuditRead
    ];
}
