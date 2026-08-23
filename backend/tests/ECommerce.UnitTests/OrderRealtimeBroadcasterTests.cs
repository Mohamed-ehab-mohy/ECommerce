using ECommerce.Domain.Events;
using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Realtime;

namespace ECommerce.UnitTests;

public sealed class OrderRealtimeBroadcasterTests
{
    private static readonly DateTime OccurredAt = new(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

    private static readonly Guid CustomerId = Guid.NewGuid();

    private static (OrderRealtimeBroadcaster Broadcaster, FakeRealtimeEventStore Store, FakeRealtimeHubContext Hub) Create()
    {
        var store = new FakeRealtimeEventStore();
        var hub = new FakeRealtimeHubContext();
        var broadcaster = new OrderRealtimeBroadcaster(new RealtimeEventForwarder(store), hub);
        return (broadcaster, store, hub);
    }

    [Fact]
    public async Task OrderStatusChanged_Pushes_Envelope_To_Customer_Group_And_Stores_It()
    {
        var (broadcaster, store, hub) = Create();

        await broadcaster.HandleAsync(
            new OrderStatusChanged(Guid.NewGuid(), "E-20260815-000001", CustomerId, OrderStatus.Placed, OrderStatus.Shipped)
            {
                OccurredOn = OccurredAt
            },
            CancellationToken.None);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal($"u:{CustomerId}", groupKey);
        Assert.Equal(RealtimeEventTypes.OrderStatusChanged, envelope.Type);
        Assert.Equal(OccurredAt, envelope.OccurredAt);
        Assert.Equal(1, envelope.EventId);

        var data = Assert.IsType<OrderStatusChangedData>(envelope.Data);
        Assert.Equal("E-20260815-000001", data.OrderNumber);
        Assert.Equal("Shipped", data.Status);

        var stored = Assert.Single(store.Events);
        Assert.Equal($"u:{CustomerId}", stored.GroupKey);
        Assert.Equal(RealtimeEventTypes.OrderStatusChanged, stored.Type);
        Assert.Contains("Shipped", stored.DataJson);
    }

    [Fact]
    public async Task OrderStatusChanged_With_No_Customer_Is_Skipped()
    {
        var (broadcaster, store, hub) = Create();

        await broadcaster.HandleAsync(
            new OrderStatusChanged(Guid.NewGuid(), "E-20260815-000002", null, OrderStatus.Placed, OrderStatus.Shipped)
            {
                OccurredOn = OccurredAt
            },
            CancellationToken.None);

        Assert.Empty(hub.Sent);
        Assert.Empty(store.Events);
    }

    [Fact]
    public async Task OrderTimelineUpdated_Pushes_Envelope_To_Customer_Group()
    {
        var (broadcaster, _, hub) = Create();

        await broadcaster.HandleAsync(
            new OrderTimelineUpdated(Guid.NewGuid(), "E-20260815-000003", CustomerId)
            {
                OccurredOn = OccurredAt
            },
            CancellationToken.None);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal($"u:{CustomerId}", groupKey);
        Assert.Equal(RealtimeEventTypes.OrderTimelineUpdated, envelope.Type);
        var data = Assert.IsType<OrderTimelineUpdatedData>(envelope.Data);
        Assert.Equal("E-20260815-000003", data.OrderNumber);
    }
}
