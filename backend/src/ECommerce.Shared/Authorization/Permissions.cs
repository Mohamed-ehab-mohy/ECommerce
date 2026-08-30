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
    public const string PlatformFlagsRead = "platform.flags.read";
    public const string PlatformFlagsWrite = "platform.flags.write";
    public const string OrdersRead = "orders.read";
    public const string OrdersSupportRead = "orders.support.read";
    public const string PromotionsRead = "promotions.read";
    public const string PromotionsWrite = "promotions.write";
    public const string FulfillmentRead = "fulfillment.read";
    public const string FulfillmentWrite = "fulfillment.write";
    public const string FinanceInvoiceRead = "finance.invoice.read";
    public const string FinanceInvoiceWrite = "finance.invoice.write";
    public const string PaymentsRefundApprove = "payments.refund.approve";
    public const string FinanceReconcile = "finance.reconcile";
    public const string ReviewsModerate = "reviews.moderate";
    public const string ReportsRead = "reports.read";
    public const string IntegrationsRead = "integrations.read";
    public const string IntegrationsWrite = "integrations.write";
    public const string ContentBannerRead = "content.banner.read";
    public const string ContentBannerWrite = "content.banner.write";
    public const string ContentBannerDelete = "content.banner.delete";
    public const string ContentPageRead = "content.page.read";
    public const string ContentPageWrite = "content.page.write";
    public const string ContentPageDelete = "content.page.delete";
    public const string ContentLayoutRead = "content.layout.read";
    public const string ContentLayoutWrite = "content.layout.write";
    public const string ContentLayoutDelete = "content.layout.delete";
    public const string CatalogCategoryWrite = "catalog.category.write";
    public const string CatalogBrandWrite = "catalog.brand.write";
    public const string PartnersWrite = "partners.write";
    public const string WalletDeposit = "wallet.deposit";
    public const string WalletConvertPoints = "wallet.convert.points";

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
        AuditRead,
        PlatformFlagsRead,
        PlatformFlagsWrite,
        OrdersRead,
        OrdersSupportRead,
        PromotionsRead,
        PromotionsWrite,
        FulfillmentRead,
        FulfillmentWrite,
        FinanceInvoiceRead,
        FinanceInvoiceWrite,
        PaymentsRefundApprove,
        FinanceReconcile,
        ReviewsModerate,
        ReportsRead,
        IntegrationsRead,
        IntegrationsWrite,
        ContentBannerRead,
        ContentBannerWrite,
        ContentBannerDelete,
        ContentPageRead,
        ContentPageWrite,
        ContentPageDelete,
        ContentLayoutRead,
        ContentLayoutWrite,
        ContentLayoutDelete,
        CatalogCategoryWrite,
        CatalogBrandWrite,
        PartnersWrite,
        WalletDeposit,
        WalletConvertPoints
    ];
}
