using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Realtime;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>
/// Periodically pushes <c>LiveOrderMetrics</c> (order rate, active stock alerts, reconciliation
/// drift) to the <c>admins</c> group on <c>adminHub</c> (US-N-003, FR-12).
/// </summary>
public sealed class LiveOpsMetricsJob(
    ECommerceDbContext dbContext,
    IRealtimeEventForwarder forwarder,
    IAdminRealtimeHubContext adminHub,
    TimeProvider timeProvider)
{
    public const string Schedule = "*/30 * * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var ordersLastFiveMinutes = await dbContext.Orders
            .CountAsync(order => order.PlacedAt >= now.AddMinutes(-5), cancellationToken);
        var ordersToday = await dbContext.Orders
            .CountAsync(order => order.PlacedAt >= now.Date, cancellationToken);
        var activeStockAlerts = await dbContext.StockItems
            .CountAsync(stock => stock.LowStockThreshold > 0 && stock.OnHand - stock.Allocated <= stock.LowStockThreshold, cancellationToken);
        var reconciliationDrifts = await dbContext.PaymentReconciliationRecords
            .CountAsync(record => record.Status == ReconciliationStatus.Drift || record.Status == ReconciliationStatus.Unmatched, cancellationToken);

        var data = new LiveOrderMetricsData(
            now,
            ordersLastFiveMinutes / 5.0,
            ordersToday,
            activeStockAlerts,
            reconciliationDrifts);

        await forwarder.ForwardAsync(adminHub, "admins", RealtimeEventTypes.LiveOrderMetrics, data, now, cancellationToken);
    }
}
