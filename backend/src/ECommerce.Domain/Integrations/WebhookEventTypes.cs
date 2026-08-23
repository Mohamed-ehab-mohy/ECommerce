namespace ECommerce.Domain.Integrations;

/// <summary>
/// Outbound webhook event catalog (docs/08 §8.2). Partner endpoints subscribe to these types
/// and receive signed deliveries (US-M-004, T-DAT-018).
/// </summary>
public static class WebhookEventTypes
{
    public const string OrderPlaced = "order.placed";
    public const string OrderPaid = "order.paid";
    public const string OrderShipped = "order.shipped";
    public const string OrderCancelled = "order.cancelled";
    public const string RefundCompleted = "refund.completed";
    public const string ProductUpdated = "product.updated";
    public const string StockLow = "stock.low";

    public static IReadOnlyList<string> All { get; } =
    [
        OrderPlaced,
        OrderPaid,
        OrderShipped,
        OrderCancelled,
        RefundCompleted,
        ProductUpdated,
        StockLow
    ];

    public static bool IsSupported(string eventType) =>
        All.Contains(eventType, StringComparer.Ordinal);
}
