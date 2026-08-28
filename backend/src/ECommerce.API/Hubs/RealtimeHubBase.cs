using System.Text.Json;
using ECommerce.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Hubs;

/// <summary>
/// Shared reconnect behavior: adds the connection to its target group and replays missed events
/// from <c>?lastEventId=&lt;opaque&gt;</c> (see docs/08-api-design.md §9).
/// </summary>
public abstract class RealtimeHubBase(IRealtimeEventStore store) : Hub
{
    private const int ReplayBatchSize = 200;

    protected async Task JoinGroupAndReplayAsync(string groupKey)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupKey, Context.ConnectionAborted);

        var lastEventId = Context.GetHttpContext()?.Request.Query["lastEventId"].ToString();
        if (!long.TryParse(lastEventId, out var after))
        {
            return;
        }

        var missed = await store.GetAfterAsync(groupKey, after, ReplayBatchSize, Context.ConnectionAborted);
        foreach (var realtimeEvent in missed)
        {
            var data = JsonSerializer.Deserialize<JsonElement>(realtimeEvent.DataJson);
            var envelope = new RealtimeEnvelope(realtimeEvent.Id, realtimeEvent.Type, realtimeEvent.OccurredAt, data);
            await Clients.Caller.SendAsync(realtimeEvent.Type, envelope, Context.ConnectionAborted);
        }
    }
}
