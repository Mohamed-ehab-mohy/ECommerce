using ECommerce.Infrastructure.Realtime;
using ECommerce.UseCases.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Hubs;

/// <summary>
/// Pushes <c>NewFulfillmentTask</c>, <c>TaskStatusChanged</c> and <c>StockAlert</c> to a warehouse's
/// <c>wh:{id}</c> group; role-gated to operations staff (US-N-002, US-N-004). The client supplies the
/// warehouse via <c>?warehouseId=</c>.
/// </summary>
[Authorize]
public sealed class WarehouseHub(IRealtimeEventStore store) : RealtimeHubBase(store)
{
    private static readonly string[] AllowedRoles = [IdentityRoles.Staff, IdentityRoles.Admin, IdentityRoles.SuperAdmin];

    public override async Task OnConnectedAsync()
    {
        var roles = Context.User?.FindAll("roles").Select(claim => claim.Value) ?? [];
        if (!roles.Intersect(AllowedRoles, StringComparer.Ordinal).Any())
        {
            Context.Abort();
            return;
        }

        var warehouseId = Context.GetHttpContext()?.Request.Query["warehouseId"].ToString();
        if (Guid.TryParse(warehouseId, out var id))
        {
            await JoinGroupAndReplayAsync($"wh:{id}");
        }

        await base.OnConnectedAsync();
    }
}
