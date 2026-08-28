using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;

namespace ECommerce.Infrastructure.Realtime;

/// <summary>
/// Fans out operational events to the <c>admins</c> group on <c>adminHub</c>: <c>StockAlerts</c>
/// and <c>ReconciliationDrift</c>. <c>LiveOrderMetrics</c> is pushed by the periodic
/// <c>LiveOpsMetricsJob</c>.
/// </summary>
public sealed class AdminRealtimeBroadcaster(
    IRealtimeEventForwarder forwarder,
    IAdminRealtimeHubContext adminHub) : IEventHandler<LowStockAlertRaised>, IEventHandler<ReconciliationDriftDetected>
{
    public Task HandleAsync(LowStockAlertRaised domainEvent, CancellationToken cancellationToken) =>
        forwarder.ForwardAsync(
            adminHub,
            "admins",
            RealtimeEventTypes.StockAlert,
            new StockAlertData(domainEvent.StockItemId, domainEvent.Sku, domainEvent.WarehouseId, domainEvent.Available, domainEvent.Threshold),
            domainEvent.OccurredOn,
            cancellationToken);

    public Task HandleAsync(ReconciliationDriftDetected domainEvent, CancellationToken cancellationToken) =>
        forwarder.ForwardAsync(
            adminHub,
            "admins",
            RealtimeEventTypes.ReconciliationDrift,
            new ReconciliationDriftData(
                domainEvent.RecordId,
                domainEvent.PaymentId,
                domainEvent.ProviderReference,
                domainEvent.Amount,
                domainEvent.Currency,
                domainEvent.Status.ToString(),
                domainEvent.Detail),
            domainEvent.OccurredOn,
            cancellationToken);
}
