using ECommerce.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Hubs;

public sealed class OrderRealtimeHubContext(IHubContext<OrderHub> hub) : IOrderRealtimeHubContext
{
    public Task SendAsync(string groupKey, RealtimeEnvelope envelope, CancellationToken cancellationToken) =>
        hub.Clients.Group(groupKey).SendAsync(envelope.Type, envelope, cancellationToken);
}

public sealed class WarehouseRealtimeHubContext(IHubContext<WarehouseHub> hub) : IWarehouseRealtimeHubContext
{
    public Task SendAsync(string groupKey, RealtimeEnvelope envelope, CancellationToken cancellationToken) =>
        hub.Clients.Group(groupKey).SendAsync(envelope.Type, envelope, cancellationToken);
}

public sealed class AdminRealtimeHubContext(IHubContext<AdminHub> hub) : IAdminRealtimeHubContext
{
    public Task SendAsync(string groupKey, RealtimeEnvelope envelope, CancellationToken cancellationToken) =>
        hub.Clients.Group(groupKey).SendAsync(envelope.Type, envelope, cancellationToken);
}
