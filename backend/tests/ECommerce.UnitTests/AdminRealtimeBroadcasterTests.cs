using ECommerce.Domain.Events;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Realtime;

namespace ECommerce.UnitTests;

public sealed class AdminRealtimeBroadcasterTests
{
    private static readonly DateTime OccurredAt = new(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

    private static (AdminRealtimeBroadcaster Broadcaster, FakeRealtimeHubContext Hub) Create()
    {
        var hub = new FakeRealtimeHubContext();
        var broadcaster = new AdminRealtimeBroadcaster(new RealtimeEventForwarder(new FakeRealtimeEventStore()), hub);
        return (broadcaster, hub);
    }

    [Fact]
    public async Task LowStockAlertRaised_Pushes_StockAlert_To_Admins()
    {
        var (broadcaster, hub) = Create();
        var stockItemId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await broadcaster.HandleAsync(
            new LowStockAlertRaised(stockItemId, "SKU-1", warehouseId, 2, 8)
            {
                OccurredOn = OccurredAt
            },
            CancellationToken.None);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal("admins", groupKey);
        Assert.Equal(RealtimeEventTypes.StockAlert, envelope.Type);
        var data = Assert.IsType<StockAlertData>(envelope.Data);
        Assert.Equal(stockItemId, data.StockItemId);
        Assert.Equal(warehouseId, data.WarehouseId);
    }

    [Fact]
    public async Task ReconciliationDriftDetected_Pushes_ReconciliationDrift_To_Admins()
    {
        var (broadcaster, hub) = Create();
        var recordId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        await broadcaster.HandleAsync(
            new ReconciliationDriftDetected(recordId, paymentId, "pi_123", 199.90m, "USD", ReconciliationStatus.Drift, "amount mismatch")
            {
                OccurredOn = OccurredAt
            },
            CancellationToken.None);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal("admins", groupKey);
        Assert.Equal(RealtimeEventTypes.ReconciliationDrift, envelope.Type);
        var data = Assert.IsType<ReconciliationDriftData>(envelope.Data);
        Assert.Equal(recordId, data.RecordId);
        Assert.Equal(paymentId, data.PaymentId);
        Assert.Equal("pi_123", data.ProviderReference);
        Assert.Equal(199.90m, data.Amount);
        Assert.Equal("USD", data.Currency);
        Assert.Equal("Drift", data.Status);
        Assert.Equal("amount mismatch", data.Detail);
    }
}
