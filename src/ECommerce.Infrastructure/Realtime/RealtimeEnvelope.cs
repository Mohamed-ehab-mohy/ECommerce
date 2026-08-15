namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Hub event type names, matching the SignalR hub contract in <c>docs/08-api-design.md</c> §9.
/// </summary>
public static class RealtimeEventTypes
{
    public const string OrderStatusChanged = "OrderStatusChanged";
    public const string OrderTimelineUpdated = "OrderTimelineUpdated";
    public const string NewFulfillmentTask = "NewFulfillmentTask";
    public const string TaskStatusChanged = "TaskStatusChanged";
    public const string StockAlert = "StockAlert";
    public const string LiveOrderMetrics = "LiveOrderMetrics";
    public const string ReconciliationDrift = "ReconciliationDrift";
}

/// <summary>
/// Wire message envelope pushed to clients (docs/08-api-design.md §9):
/// <c>{ eventId, type, occurredAt, data }</c>.
/// </summary>
public sealed record RealtimeEnvelope(
    long EventId,
    string Type,
    DateTime OccurredAt,
    object? Data);
