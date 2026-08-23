using ECommerce.Domain.Integrations;
using ECommerce.UseCases.Integrations.Responses;

namespace ECommerce.UnitTests.Tests;

public sealed class WebhookContractTests
{
    [Fact]
    public void CreateEndpoint_Response_HasRequiredFields()
    {
        var response = new WebhookEndpointCreatedResponse(
            Guid.NewGuid(),
            "Order Events",
            "https://example.com/webhook",
            "whsec_abc123",
            [WebhookEventTypes.OrderPlaced, WebhookEventTypes.OrderShipped]);

        Assert.NotEqual(Guid.Empty, response.EndpointId);
        Assert.Equal("Order Events", response.Name);
        Assert.StartsWith("https://", response.Url);
        Assert.StartsWith("whsec_", response.Secret);
        Assert.NotEmpty(response.Secret);
        Assert.Equal(2, response.EventTypes.Count);
        Assert.Contains(WebhookEventTypes.OrderPlaced, response.EventTypes);
        Assert.Contains(WebhookEventTypes.OrderShipped, response.EventTypes);
    }

    [Fact]
    public void ListEndpoints_Response_HasRequiredFields()
    {
        var response = new WebhookEndpointResponse(
            Guid.NewGuid(),
            "Payment Events",
            "https://partner.example.com/hook",
            true,
            null,
            [WebhookEventTypes.OrderPaid, WebhookEventTypes.RefundCompleted]);

        Assert.NotEqual(Guid.Empty, response.EndpointId);
        Assert.Equal("Payment Events", response.Name);
        Assert.Equal("https://partner.example.com/hook", response.Url);
        Assert.True(response.IsActive);
        Assert.Null(response.SuspendedUntilUtc);
        Assert.Equal(2, response.EventTypes.Count);
    }

    [Fact]
    public void ListEndpoints_Suspended_HasSuspendedUntilUtc()
    {
        var suspendedAt = DateTime.UtcNow.AddHours(1);
        var response = new WebhookEndpointResponse(
            Guid.NewGuid(),
            "Suspended Endpoint",
            "https://example.com/hook",
            false,
            suspendedAt,
            [WebhookEventTypes.OrderPlaced]);

        Assert.False(response.IsActive);
        Assert.Equal(suspendedAt, response.SuspendedUntilUtc);
    }

    [Fact]
    public void RotateSecret_Response_HasEndpointId_And_NewSecret()
    {
        var response = new WebhookSecretRotatedResponse(
            Guid.NewGuid(),
            "whsec_new_secret_456");

        Assert.NotEqual(Guid.Empty, response.EndpointId);
        Assert.StartsWith("whsec_", response.Secret);
    }

    [Fact]
    public void Replay_Response_HasReplayedCount()
    {
        var response = new WebhookReplayResponse(3);
        Assert.Equal(3, response.Replayed);
    }

    [Fact]
    public void Delivery_HasRequiredFields()
    {
        var deliveredAt = DateTime.UtcNow;
        var response = new WebhookDeliveryResponse(
            DeliveryId: Guid.NewGuid(),
            EndpointId: Guid.NewGuid(),
            EventId: "evt_abc123",
            EventType: WebhookEventTypes.OrderPlaced,
            Status: "Delivered",
            Attempts: 1,
            LastStatusCode: 200,
            LastError: null,
            NextRetryAtUtc: null,
            DeliveredAtUtc: deliveredAt);

        Assert.NotEqual(Guid.Empty, response.DeliveryId);
        Assert.NotEqual(Guid.Empty, response.EndpointId);
        Assert.StartsWith("evt_", response.EventId);
        Assert.Equal(WebhookEventTypes.OrderPlaced, response.EventType);
        Assert.Equal("Delivered", response.Status);
        Assert.Equal(1, response.Attempts);
        Assert.Equal(200, response.LastStatusCode);
        Assert.Null(response.LastError);
        Assert.Null(response.NextRetryAtUtc);
        Assert.NotNull(response.DeliveredAtUtc);
    }

    [Fact]
    public void Delivery_Pending_HasNextRetry()
    {
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        var response = new WebhookDeliveryResponse(
            DeliveryId: Guid.NewGuid(),
            EndpointId: Guid.NewGuid(),
            EventId: "evt_def456",
            EventType: WebhookEventTypes.OrderShipped,
            Status: "Pending",
            Attempts: 1,
            LastStatusCode: 503,
            LastError: "Service Unavailable",
            NextRetryAtUtc: nextRetry,
            DeliveredAtUtc: null);

        Assert.Equal("Pending", response.Status);
        Assert.Equal(503, response.LastStatusCode);
        Assert.Equal("Service Unavailable", response.LastError);
        Assert.Equal(nextRetry, response.NextRetryAtUtc);
        Assert.Null(response.DeliveredAtUtc);
    }

    [Fact]
    public void Delivery_Failed_HasNullNextRetry()
    {
        var response = new WebhookDeliveryResponse(
            DeliveryId: Guid.NewGuid(),
            EndpointId: Guid.NewGuid(),
            EventId: "evt_ghi789",
            EventType: WebhookEventTypes.RefundCompleted,
            Status: "Failed",
            Attempts: 5,
            LastStatusCode: 410,
            LastError: "Gone",
            NextRetryAtUtc: null,
            DeliveredAtUtc: null);

        Assert.Equal("Failed", response.Status);
        Assert.Equal(5, response.Attempts);
        Assert.Null(response.NextRetryAtUtc);
    }

    [Fact]
    public void WebhookEventTypes_All_AreSnakeCase()
    {
        foreach (var eventType in WebhookEventTypes.All)
        {
            Assert.Contains('.', eventType);
            Assert.DoesNotContain(' ', eventType);
            Assert.Equal(eventType.ToLowerInvariant(), eventType);
        }
    }

    [Fact]
    public void WebhookEventTypes_IsSupported_ReturnsTrue_ForAll()
    {
        foreach (var eventType in WebhookEventTypes.All)
        {
            Assert.True(WebhookEventTypes.IsSupported(eventType));
        }
    }

    [Fact]
    public void WebhookEventTypes_IsSupported_ReturnsFalse_ForUnknown()
    {
        Assert.False(WebhookEventTypes.IsSupported("unknown.event"));
        Assert.False(WebhookEventTypes.IsSupported(""));
    }

    [Fact]
    public void WebhookEndpoint_Subscription_Flow()
    {
        var endpoint = WebhookEndpoint.Create(
            "My Integration",
            "https://myapp.com/webhooks",
            "whsec_test123",
            [WebhookEventTypes.OrderPlaced, WebhookEventTypes.OrderShipped],
            DateTime.UtcNow);

        Assert.Equal("My Integration", endpoint.Name);
        Assert.Equal("https://myapp.com/webhooks", endpoint.Url);
        Assert.True(endpoint.IsActive);
        Assert.Equal(2, endpoint.EventTypes.Count);
        Assert.True(endpoint.IsSubscribedTo(WebhookEventTypes.OrderPlaced));
        Assert.False(endpoint.IsSubscribedTo(WebhookEventTypes.RefundCompleted));
    }

    [Fact]
    public void WebhookEndpoint_Suspend_Resume_Flow()
    {
        var now = DateTime.UtcNow;
        var endpoint = WebhookEndpoint.Create(
            "Test",
            "https://test.com",
            "whsec_test",
            [WebhookEventTypes.OrderPlaced],
            now);

        endpoint.Suspend(now);
        Assert.True(endpoint.IsSuspended(now));
        Assert.True(endpoint.IsActive);

        endpoint.Resume(now);
        Assert.False(endpoint.IsSuspended(now));
        Assert.True(endpoint.IsActive);
    }

    [Fact]
    public void WebhookDelivery_RecordSuccess_SetsDeliveredStatus()
    {
        var delivery = WebhookDelivery.Create(
            Guid.NewGuid(),
            "evt_001",
            WebhookEventTypes.OrderPlaced,
            "{}",
            DateTime.UtcNow);

        var now = DateTime.UtcNow;
        delivery.RecordSuccess(200, now);

        Assert.Equal(WebhookDeliveryStatus.Delivered, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(200, delivery.LastStatusCode);
        Assert.NotNull(delivery.DeliveredAtUtc);
    }

    [Fact]
    public void WebhookDelivery_RecordFailure_SetsPendingForRetry()
    {
        var delivery = WebhookDelivery.Create(
            Guid.NewGuid(),
            "evt_002",
            WebhookEventTypes.OrderShipped,
            "{}",
            DateTime.UtcNow);

        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        delivery.RecordFailure(503, "Service Unavailable", nextRetry, DateTime.UtcNow);

        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(503, delivery.LastStatusCode);
        Assert.NotNull(delivery.NextRetryAtUtc);
    }

    [Fact]
    public void WebhookDelivery_RecordFailure_SetsFailedWhenNoRetry()
    {
        var delivery = WebhookDelivery.Create(
            Guid.NewGuid(),
            "evt_003",
            WebhookEventTypes.RefundCompleted,
            "{}",
            DateTime.UtcNow);

        delivery.RecordFailure(410, "Gone", null, DateTime.UtcNow);

        Assert.Equal(WebhookDeliveryStatus.Failed, delivery.Status);
        Assert.Null(delivery.NextRetryAtUtc);
    }
}
