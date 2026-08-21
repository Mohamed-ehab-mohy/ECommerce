namespace ECommerce.UseCases.Integrations.Ports;

public sealed record WebhookDeadLetterEntryDto(
    Guid Id,
    Guid DeliveryId,
    Guid EndpointId,
    string EventType,
    string EventId,
    string PayloadJson,
    string EndpointUrl,
    string EndpointName,
    int TotalAttempts,
    int? LastStatusCode,
    string ErrorReason,
    DateTime FirstFailedAtUtc,
    DateTime LastFailedAtUtc,
    bool IsReplayed,
    DateTime? ReplayedAtUtc);

public interface IWebhookDeadLetterRepository
{
    Task<WebhookDeadLetterEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookDeadLetterEntryDto>> ListAsync(int limit, int offset, string? eventType, CancellationToken cancellationToken);

    Task<int> CountAsync(string? eventType, CancellationToken cancellationToken);

    void Add(WebhookDeadLetterEntryDto entry);

    Task<bool> ExistsForDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken);

    Task MarkReplayedAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> MarkDeliveryReplayedAsync(Guid entryId, DateTime utcNow, CancellationToken cancellationToken);
}

public static class WebhookEventTypesCatalog
{
    public const string OrderPlaced = "order.placed";
    public const string OrderPaid = "order.paid";
    public const string OrderShipped = "order.shipped";
    public const string OrderCancelled = "order.cancelled";
    public const string RefundCompleted = "refund.completed";
    public const string ProductUpdated = "product.updated";
    public const string StockLow = "stock.low";
}
