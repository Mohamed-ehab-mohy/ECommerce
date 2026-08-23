using ECommerce.Infrastructure.Realtime;

namespace ECommerce.UnitTests;

public sealed class RealtimeEventForwarderTests
{
    [Fact]
    public async Task ForwardAsync_Appends_Then_Pushes_Envelope_With_Store_EventId()
    {
        var store = new FakeRealtimeEventStore();
        var hub = new FakeRealtimeHubContext();
        var forwarder = new RealtimeEventForwarder(store);
        var occurredAt = new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

        await forwarder.ForwardAsync(
            hub,
            "u:11111111-1111-1111-1111-111111111111",
            RealtimeEventTypes.OrderStatusChanged,
            new OrderStatusChangedData("E-20260815-000001", "Placed"),
            occurredAt,
            CancellationToken.None);

        var stored = Assert.Single(store.Events);
        Assert.Equal("u:11111111-1111-1111-1111-111111111111", stored.GroupKey);
        Assert.Equal(RealtimeEventTypes.OrderStatusChanged, stored.Type);
        Assert.Equal(occurredAt, stored.OccurredAt);

        var (groupKey, envelope) = Assert.Single(hub.Sent);
        Assert.Equal("u:11111111-1111-1111-1111-111111111111", groupKey);
        Assert.Equal(stored.Id, envelope.EventId);
        Assert.Equal(RealtimeEventTypes.OrderStatusChanged, envelope.Type);
        Assert.Equal(occurredAt, envelope.OccurredAt);

        var data = Assert.IsType<OrderStatusChangedData>(envelope.Data);
        Assert.Equal("E-20260815-000001", data.OrderNumber);
    }

    [Fact]
    public async Task ForwardAsync_Sequential_Events_Get_Monotonic_Ids()
    {
        var store = new FakeRealtimeEventStore();
        var hub = new FakeRealtimeHubContext();
        var forwarder = new RealtimeEventForwarder(store);

        await forwarder.ForwardAsync(hub, "admins", RealtimeEventTypes.StockAlert, new StockAlertData(Guid.NewGuid(), "SKU-1", Guid.NewGuid(), 1, 5), DateTime.UtcNow, CancellationToken.None);
        await forwarder.ForwardAsync(hub, "admins", RealtimeEventTypes.StockAlert, new StockAlertData(Guid.NewGuid(), "SKU-2", Guid.NewGuid(), 2, 5), DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, store.Events.Count);
        Assert.Equal([1L, 2L], store.Events.Select(realtimeEvent => realtimeEvent.Id).ToArray());
        Assert.Equal([1L, 2L], hub.Sent.Select(sent => sent.Envelope.EventId).ToArray());
    }
}
