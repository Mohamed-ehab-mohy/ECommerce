namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Payload data records pushed inside <see cref="RealtimeEnvelope"/>; serialized to JSON for
/// both the wire and the replay store.
/// </summary>
public sealed record OrderStatusChangedData(string OrderNumber, string Status);

public sealed record OrderTimelineUpdatedData(string OrderNumber);

public sealed record NewFulfillmentTaskData(Guid TaskId, Guid OrderId, string? Zone, int Priority);

public sealed record TaskStatusChangedData(Guid TaskId, Guid OrderId, string Status);

public sealed record StockAlertData(Guid StockItemId, string Sku, Guid WarehouseId, int Available, int Threshold);

public sealed record LiveOrderMetricsData(
    DateTime TimestampUtc,
    double OrdersPerMinute,
    int OrdersToday,
    int ActiveStockAlerts,
    int ReconciliationDrifts);

public sealed record ReconciliationDriftData(
    Guid RecordId,
    Guid PaymentId,
    string ProviderReference,
    decimal Amount,
    string Currency,
    string Status,
    string Detail);
