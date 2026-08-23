namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Appends a real-time event to the replay store, then pushes the envelope to the target group.
/// </summary>
public interface IRealtimeEventForwarder
{
    Task ForwardAsync(
        IRealtimeHubContext hub,
        string groupKey,
        string type,
        object data,
        DateTime occurredAt,
        CancellationToken cancellationToken);
}
