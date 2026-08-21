using ECommerce.Domain.Integrations;

namespace ECommerce.UnitTests.Tests.WebhookContracts;

public sealed class WebhookDeadLetterTests
{
    [Fact]
    public void DeadLetterEntry_Create_SetsAllFields()
    {
        var now = DateTime.UtcNow;
        var entry = WebhookDeadLetterEntry.Create(
            deliveryId: Guid.NewGuid(),
            endpointId: Guid.NewGuid(),
            eventType: WebhookEventTypes.OrderPlaced,
            eventId: "evt_001",
            payloadJson: "{}",
            endpointUrl: "https://example.com/webhook",
            endpointName: "Test Partner",
            totalAttempts: 5,
            lastStatusCode: 503,
            errorReason: "Service Unavailable",
            utcNow: now);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.NotEqual(Guid.Empty, entry.DeliveryId);
        Assert.NotEqual(Guid.Empty, entry.EndpointId);
        Assert.Equal(WebhookEventTypes.OrderPlaced, entry.EventType);
        Assert.Equal("evt_001", entry.EventId);
        Assert.Equal("{}", entry.PayloadJson);
        Assert.Equal("https://example.com/webhook", entry.EndpointUrl);
        Assert.Equal("Test Partner", entry.EndpointName);
        Assert.Equal(5, entry.TotalAttempts);
        Assert.Equal(503, entry.LastStatusCode);
        Assert.Equal("Service Unavailable", entry.ErrorReason);
        Assert.Equal(now, entry.FirstFailedAtUtc);
        Assert.Equal(now, entry.LastFailedAtUtc);
        Assert.False(entry.IsReplayed);
        Assert.Null(entry.ReplayedAtUtc);
    }

    [Fact]
    public void DeadLetterEntry_MarkReplayed_SetsReplayedAtUtc()
    {
        var entry = CreateSampleEntry();
        var now = DateTime.UtcNow;

        entry.MarkReplayed(now);

        Assert.True(entry.IsReplayed);
        Assert.Equal(now, entry.ReplayedAtUtc);
    }

    [Fact]
    public void DeadLetterEntry_IsReplayed_False_ByDefault()
    {
        var entry = CreateSampleEntry();
        Assert.False(entry.IsReplayed);
    }

    [Fact]
    public void DeadLetterEntry_AllEventTypes_AreSupported()
    {
        foreach (var eventType in WebhookEventTypes.All)
        {
            var entry = WebhookDeadLetterEntry.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                eventType,
                $"evt_{eventType}",
                "{}",
                "https://example.com",
                "Partner",
                5,
                500,
                "Error",
                DateTime.UtcNow);

            Assert.Equal(eventType, entry.EventType);
        }
    }

    [Fact]
    public void DeadLetterEntry_RecordEquality_Works()
    {
        var deliveryId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var e1 = WebhookDeadLetterEntry.Create(deliveryId, endpointId, "order.placed", "evt_001", "{}", "https://example.com", "P", 5, 500, "err", now);
        var e2 = WebhookDeadLetterEntry.Create(deliveryId, endpointId, "order.placed", "evt_001", "{}", "https://example.com", "P", 5, 500, "err", now);

        Assert.NotEqual(e1.Id, e2.Id);
        Assert.Equal(e1.DeliveryId, e2.DeliveryId);
        Assert.Equal(e1.EndpointId, e2.EndpointId);
        Assert.Equal(e1.EventType, e2.EventType);
        Assert.Equal(e1.ErrorReason, e2.ErrorReason);
    }

    private static WebhookDeadLetterEntry CreateSampleEntry() =>
        WebhookDeadLetterEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            WebhookEventTypes.OrderPlaced,
            "evt_sample",
            "{}",
            "https://example.com/webhook",
            "Test Partner",
            5,
            503,
            "Service Unavailable",
            DateTime.UtcNow);
}
