using ECommerce.Infrastructure.Realtime;
using ECommerce.UseCases.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Hubs;

/// <summary>
/// Pushes <c>LiveOrderMetrics</c>, <c>StockAlerts</c> and <c>ReconciliationDrift</c> to the <c>admins</c>
/// group; gated to Admin/SuperAdmin roles.
/// </summary>
[Authorize]
public sealed class AdminHub(IRealtimeEventStore store) : RealtimeHubBase(store)
{
    public override async Task OnConnectedAsync()
    {
        var roles = Context.User?.FindAll("roles").Select(claim => claim.Value) ?? [];
        if (!roles.Contains(IdentityRoles.Admin, StringComparer.Ordinal)
            && !roles.Contains(IdentityRoles.SuperAdmin, StringComparer.Ordinal))
        {
            Context.Abort();
            return;
        }

        await JoinGroupAndReplayAsync("admins");

        await base.OnConnectedAsync();
    }
}
