using ECommerce.Domain.Integrations;

namespace ECommerce.UnitTests;

public sealed class WebhookDeliveryTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private static WebhookDelivery CreateDelivery() =>
        WebhookDelivery.Create(Guid.NewGuid(), "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);

    [Fact]
    public void Create_Starts_Pending_With_No_Attempts()
    {
        var endpointId = Guid.NewGuid();
        var delivery = WebhookDelivery.Create(endpointId, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);

        Assert.Equal(endpointId, delivery.EndpointId);
        Assert.Equal("evt_1", delivery.EventId);
        Assert.Equal("{}", delivery.PayloadJson);
        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.Attempts);
        Assert.Null(delivery.NextRetryAtUtc);
        Assert.Null(delivery.DeliveredAtUtc);
    }

    [Fact]
    public void RecordSuccess_Marks_Delivered()
    {
        var delivery = CreateDelivery();

        delivery.RecordSuccess(200, UtcNow);

        Assert.Equal(WebhookDeliveryStatus.Delivered, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(200, delivery.LastStatusCode);
        Assert.Null(delivery.LastError);
        Assert.Null(delivery.NextRetryAtUtc);
        Assert.Equal(UtcNow, delivery.DeliveredAtUtc);
    }

    [Fact]
    public void RecordFailure_With_NextRetry_Stays_Pending()
    {
        var delivery = CreateDelivery();
        var nextRetry = UtcNow.AddMinutes(1);

        delivery.RecordFailure(503, "boom", nextRetry, UtcNow);

        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(503, delivery.LastStatusCode);
        Assert.Equal("boom", delivery.LastError);
        Assert.Equal(nextRetry, delivery.NextRetryAtUtc);
        Assert.Null(delivery.DeliveredAtUtc);
    }

    [Fact]
    public void RecordFailure_Without_NextRetry_Marks_Failed()
    {
        var delivery = CreateDelivery();

        delivery.RecordFailure(null, "boom", null, UtcNow);

        Assert.Equal(WebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Null(delivery.NextRetryAtUtc);
    }

    [Fact]
    public void Suspend_Marks_Delivery_Suspended()
    {
        var delivery = CreateDelivery();

        delivery.Suspend("gave up", UtcNow);

        Assert.Equal(WebhookDeliveryStatus.Suspended, delivery.Status);
        Assert.Equal("gave up", delivery.LastError);
        Assert.Null(delivery.NextRetryAtUtc);
    }

    [Fact]
    public void ResetForReplay_Rewinds_To_Pending()
    {
        var delivery = CreateDelivery();
        delivery.RecordFailure(500, "boom", null, UtcNow);

        delivery.ResetForReplay(UtcNow.AddMinutes(5));

        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.Attempts);
        Assert.Null(delivery.LastStatusCode);
        Assert.Null(delivery.LastError);
        Assert.Null(delivery.NextRetryAtUtc);
        Assert.Null(delivery.DeliveredAtUtc);
    }
}
