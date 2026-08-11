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
    public const string WarehouseCreated = "inventory.warehouse.created";
    public const string WarehouseUpdated = "inventory.warehouse.updated";
    public const string WarehouseDeactivated = "inventory.warehouse.deactivated";
    public const string StockMovementPosted = "inventory.stock.movement.posted";
    public const string RoleCreated = "identity.role.created";
    public const string RolePermissionsChanged = "identity.role.permissions.changed";
    public const string RoleAssigned = "identity.role.assigned";
    public const string FeatureFlagChanged = "platform.feature.flag.changed";
    public const string NotificationPreferenceUpdated = "notifications.preference.updated";
    public const string PromotionCreated = "promotions.promotion.created";
    public const string PromotionUpdated = "promotions.promotion.updated";
    public const string PromotionActivated = "promotions.promotion.activated";
    public const string PromotionPaused = "promotions.promotion.paused";
    public const string PromotionScheduled = "promotions.promotion.scheduled";
    public const string CouponCreated = "promotions.coupon.created";
}
