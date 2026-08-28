using ECommerce.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Hubs;

/// <summary>
/// Pushes <c>OrderStatusChanged</c> and <c>OrderTimelineUpdated</c> to the authenticated customer's
/// <c>u:{userId}</c> group.
/// </summary>
[Authorize]
public sealed class OrderHub(IRealtimeEventStore store) : RealtimeHubBase(store)
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (Guid.TryParse(userId, out var id))
        {
            await JoinGroupAndReplayAsync($"u:{id}");
        }

        await base.OnConnectedAsync();
    }
}
