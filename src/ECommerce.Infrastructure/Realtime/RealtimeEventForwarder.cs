using System.Text.Json;

namespace ECommerce.Infrastructure.Realtime;

public sealed class RealtimeEventForwarder(IRealtimeEventStore store) : IRealtimeEventForwarder
{
    public async Task ForwardAsync(
        IRealtimeHubContext hub,
        string groupKey,
        string type,
        object data,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var realtimeEvent = new RealtimeEvent
        {
            GroupKey = groupKey,
            Type = type,
            DataJson = JsonSerializer.Serialize(data),
            OccurredAt = occurredAt
        };

        var eventId = await store.AppendAsync(realtimeEvent, cancellationToken);
        await hub.SendAsync(groupKey, new RealtimeEnvelope(eventId, type, occurredAt, data), cancellationToken);
    }
}
