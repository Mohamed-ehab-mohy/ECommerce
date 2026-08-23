namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Persisted real-time event used for reconnect replay (T-DAT-016). One row per delivered
/// envelope, keyed by the target group (e.g. <c>u:{userId}</c>, <c>wh:{warehouseId}</c>, <c>admins</c>).
/// </summary>
public sealed class RealtimeEvent
{
    public long Id { get; set; }

    public string GroupKey { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string DataJson { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}
