namespace ECommerce.Domain.Notifications;

public enum NotificationKind
{
    OrderConfirmation,
    OrderStatusUpdate,
    LowStockAlert,
    WebhookSuspended
}
