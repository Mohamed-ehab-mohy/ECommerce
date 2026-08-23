namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Gateway from infrastructure event handlers to a SignalR hub, so Infrastructure never depends
/// on ASP.NET Core. The API project implements one per hub.
/// </summary>
public interface IRealtimeHubContext
{
    Task SendAsync(string groupKey, RealtimeEnvelope envelope, CancellationToken cancellationToken);
}

public interface IOrderRealtimeHubContext : IRealtimeHubContext;

public interface IWarehouseRealtimeHubContext : IRealtimeHubContext;

public interface IAdminRealtimeHubContext : IRealtimeHubContext;
