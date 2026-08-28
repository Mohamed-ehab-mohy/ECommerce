namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Stores and replays real-time events per target group so clients can resume missed events
/// on reconnect via <c>?lastEventId=</c> (see docs/08-api-design.md §9).
/// </summary>
public interface IRealtimeEventStore
{
    Task<long> AppendAsync(RealtimeEvent realtimeEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<RealtimeEvent>> GetAfterAsync(
        string groupKey,
        long lastEventId,
        int take,
        CancellationToken cancellationToken);
}
