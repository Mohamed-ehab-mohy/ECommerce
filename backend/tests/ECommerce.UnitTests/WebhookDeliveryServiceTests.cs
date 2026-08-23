using ECommerce.Domain.Integrations;
using ECommerce.Domain.Notifications;
using ECommerce.Infrastructure.Notifications;
using ECommerce.UseCases.Integrations.Ports;
using ECommerce.UseCases.Integrations.Services;
using ECommerce.UseCases.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests;

public sealed class WebhookDeliveryServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeWebhookEndpointRepository _endpoints = new();

    private readonly FakeWebhookDeliveryRepository _deliveries = new();

    private readonly FakeWebhookDeliveryJobScheduler _scheduler = new();

    private readonly FakeWebhookSigner _signer = new();

    private readonly FakeWebhookHttpDeliverer _http = new();

    private readonly FakeNotificationPreferenceRepository _preferences = new();

    private readonly FakeNotificationQueue _queue = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private WebhookDeliveryService CreateService(WebhookOptions? options = null) =>
        new(
            _endpoints,
            _deliveries,
            _scheduler,
            _signer,
            _http,
            new NotificationDispatcher(
                _preferences,
                new InMemoryNotificationTemplateStore(),
                _queue,
                NullLogger<NotificationDispatcher>.Instance),
            Options.Create(options ?? new WebhookOptions()),
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            NullLogger<WebhookDeliveryService>.Instance);

    private WebhookEndpoint AddEndpoint(params string[] eventTypes)
    {
        var endpoint = WebhookEndpoint.Create(
            "Partner",
            "https://partner.test/hook",
            "secret",
            eventTypes.Length == 0 ? [WebhookEventTypes.OrderPlaced] : eventTypes,
            UtcNow);
        _endpoints.Add(endpoint);
        return endpoint;
    }

    [Fact]
    public async Task Dispatch_No_Subscribers_Is_Noop()
    {
        var service = CreateService();

        await service.DispatchAsync(UtcNow, WebhookEventTypes.OrderPlaced, new { }, CancellationToken.None);

        Assert.Empty(_deliveries.Deliveries);
        Assert.Empty(_scheduler.Enqueued);
    }

    [Fact]
    public async Task Dispatch_Creates_Pending_Delivery_And_Enqueues_Job()
    {
        var endpoint = AddEndpoint();
        var service = CreateService();

        await service.DispatchAsync(UtcNow, WebhookEventTypes.OrderPlaced, new { orderId = 1 }, CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(endpoint.Id, delivery.EndpointId);
        Assert.Equal(WebhookEventTypes.OrderPlaced, delivery.EventType);
        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.Attempts);
        Assert.StartsWith("evt_", delivery.EventId);
        Assert.Contains("order.placed", delivery.PayloadJson);
        Assert.Equal(delivery.Id, Assert.Single(_scheduler.Enqueued));
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Dispatch_Skips_Suspended_Endpoints()
    {
        var endpoint = AddEndpoint();
        endpoint.Suspend(UtcNow);
        var service = CreateService();

        await service.DispatchAsync(UtcNow, WebhookEventTypes.OrderPlaced, new { }, CancellationToken.None);

        Assert.Empty(_deliveries.Deliveries);
    }

    [Fact]
    public async Task Deliver_Success_Records_Delivery()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        _deliveries.Add(delivery);
        _http.Result = new WebhookDeliveryResult(true, 200, null);
        var service = CreateService();

        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        Assert.Equal(WebhookDeliveryStatus.Delivered, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(200, delivery.LastStatusCode);
        Assert.Null(delivery.NextRetryAtUtc);
        Assert.Equal(UtcNow, delivery.DeliveredAtUtc);
        var call = Assert.Single(_http.Calls);
        Assert.Equal("https://partner.test/hook", call.Url);
        Assert.Equal(delivery.EventId, call.EventId);
        Assert.Equal(delivery.PayloadJson, call.PayloadJson);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deliver_Failure_Schedules_Exponential_Backoff()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        _deliveries.Add(delivery);
        _http.Result = new WebhookDeliveryResult(false, 503, "unavailable");
        var service = CreateService();

        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(503, delivery.LastStatusCode);
        Assert.Equal(UtcNow.AddMinutes(1), delivery.NextRetryAtUtc);
        var (_, delay) = Assert.Single(_scheduler.Scheduled);
        Assert.Equal(TimeSpan.FromMinutes(1), delay);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deliver_Http_Exception_Is_Treated_As_Failure()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        _deliveries.Add(delivery);
        _http.Exception = new InvalidOperationException("network down");
        var service = CreateService();

        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Contains("network down", delivery.LastError);
        Assert.Equal(UtcNow.AddMinutes(1), delivery.NextRetryAtUtc);
    }

    [Fact]
    public async Task Deliver_Backoff_Doubles_Each_Attempt()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        _deliveries.Add(delivery);
        _http.Result = new WebhookDeliveryResult(false, 500, "boom");
        var service = CreateService();

        await service.DeliverAsync(delivery.Id, CancellationToken.None);
        await service.DeliverAsync(delivery.Id, CancellationToken.None);
        await service.DeliverAsync(delivery.Id, CancellationToken.None);
        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        var delays = _scheduler.Scheduled.Select(entry => entry.Delay).ToList();
        Assert.Equal(
            [
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(4),
                TimeSpan.FromMinutes(8)
            ],
            delays);
    }

    [Fact]
    public async Task Deliver_Max_Attempts_Suspends_Endpoint_And_Notifies_Ops()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        _deliveries.Add(delivery);
        _http.Result = new WebhookDeliveryResult(false, 500, "boom");
        var service = CreateService(new WebhookOptions { MaxAttempts = 2 });

        await service.DeliverAsync(delivery.Id, CancellationToken.None);
        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        Assert.Equal(WebhookDeliveryStatus.Suspended, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.True(endpoint.IsSuspended(UtcNow));
        var (_, delay) = Assert.Single(_scheduler.Scheduled);
        Assert.Equal(TimeSpan.FromMinutes(1), delay);

        var envelope = Assert.Single(_queue.Envelopes);
        Assert.Equal(NotificationKind.WebhookSuspended, envelope.Kind);
        Assert.Equal("ops@ecommerce.dev", envelope.Recipient);
        Assert.Contains(endpoint.Name, envelope.Subject);
    }

    [Fact]
    public async Task Deliver_Missing_Delivery_Is_Skipped()
    {
        var service = CreateService();

        await service.DeliverAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(_http.Calls);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deliver_Already_Delivered_Is_Skipped()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        delivery.RecordSuccess(200, UtcNow);
        _deliveries.Add(delivery);
        var service = CreateService();

        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        Assert.Empty(_http.Calls);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deliver_Suspended_Endpoint_Is_Skipped()
    {
        var endpoint = AddEndpoint();
        endpoint.Suspend(UtcNow);
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        _deliveries.Add(delivery);
        var service = CreateService();

        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        Assert.Empty(_http.Calls);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Deliver_Inactive_Endpoint_Is_Skipped()
    {
        var endpoint = AddEndpoint();
        endpoint.Deactivate(UtcNow);
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        _deliveries.Add(delivery);
        var service = CreateService();

        await service.DeliverAsync(delivery.Id, CancellationToken.None);

        Assert.Empty(_http.Calls);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }
}
