using ECommerce.Domain.Events;
using ECommerce.Domain.Integrations;
using ECommerce.Domain.Notifications;
using ECommerce.Infrastructure.Notifications;
using ECommerce.UseCases.Integrations.Services;
using ECommerce.UseCases.Notifications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests;

public sealed class WebhookEventDispatcherTests
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

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeProductRepository _products = new();

    private readonly FakeWarehouseRepository _warehouses = new();

    private WebhookEventDispatcher CreateDispatcher()
    {
        var service = new WebhookDeliveryService(
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
            Options.Create(new WebhookOptions()),
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            NullLogger<WebhookDeliveryService>.Instance);

        return new WebhookEventDispatcher(service, _orders, _products, _warehouses);
    }

    private WebhookEndpoint SubscribeTo(string eventType)
    {
        var endpoint = WebhookEndpoint.Create("Partner", "https://partner.test/hook", "secret", [eventType], UtcNow);
        _endpoints.Add(endpoint);
        return endpoint;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task OrderPlaced_Maps_To_Order_Placed_Event()
    {
        SubscribeTo(WebhookEventTypes.OrderPlaced);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new OrderPlaced(Guid.NewGuid(), "E-1", Guid.NewGuid(), Guid.NewGuid(), "a@b.c", 39.90m, "USD"),
            CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(WebhookEventTypes.OrderPlaced, delivery.EventType);
        Assert.Contains("order.placed", delivery.PayloadJson);
        Assert.Equal(delivery.Id, Assert.Single(_scheduler.Enqueued));
    }

    [Fact]
    public async Task PaymentCaptured_Maps_To_Order_Paid_Event()
    {
        SubscribeTo(WebhookEventTypes.OrderPaid);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new PaymentCaptured(Guid.NewGuid(), null, 39.90m, "USD"),
            CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(WebhookEventTypes.OrderPaid, delivery.EventType);
    }

    [Fact]
    public async Task OrderShipped_Maps_To_Order_Shipped_Event()
    {
        SubscribeTo(WebhookEventTypes.OrderShipped);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new OrderShipped(Guid.NewGuid(), "E-1", "a@b.c", "aramex", ["1Z999"]),
            CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(WebhookEventTypes.OrderShipped, delivery.EventType);
        Assert.Contains("1Z999", delivery.PayloadJson);
    }

    [Fact]
    public async Task OrderCancelled_Maps_To_Order_Cancelled_Event()
    {
        SubscribeTo(WebhookEventTypes.OrderCancelled);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new OrderCancelled(Guid.NewGuid(), "E-1", "a@b.c", 39.90m, "USD", "customer-request"),
            CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(WebhookEventTypes.OrderCancelled, delivery.EventType);
        Assert.Contains("customer-request", delivery.PayloadJson);
    }

    [Fact]
    public async Task RefundCompleted_Maps_To_Refund_Completed_Event()
    {
        SubscribeTo(WebhookEventTypes.RefundCompleted);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new RefundCompleted(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10.00m, "USD", null),
            CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(WebhookEventTypes.RefundCompleted, delivery.EventType);
    }

    [Fact]
    public async Task ProductUpdated_Maps_To_Product_Updated_Event()
    {
        SubscribeTo(WebhookEventTypes.ProductUpdated);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new ProductUpdated(Guid.NewGuid(), "SKU-1", "widget", "Widget", "USD", 10m, null),
            CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(WebhookEventTypes.ProductUpdated, delivery.EventType);
        Assert.Contains("SKU-1", delivery.PayloadJson);
    }

    [Fact]
    public async Task LowStockAlert_Maps_To_Stock_Low_Event()
    {
        SubscribeTo(WebhookEventTypes.StockLow);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new LowStockAlertRaised(Guid.NewGuid(), "SKU-1", Guid.NewGuid(), 3, 5),
            CancellationToken.None);

        var delivery = Assert.Single(_deliveries.Deliveries);
        Assert.Equal(WebhookEventTypes.StockLow, delivery.EventType);
    }

    [Fact]
    public async Task Unsubscribed_Event_Creates_No_Delivery()
    {
        SubscribeTo(WebhookEventTypes.OrderPlaced);
        var dispatcher = CreateDispatcher();

        await dispatcher.HandleAsync(
            new OrderShipped(Guid.NewGuid(), "E-1", "a@b.c", "aramex", ["1Z999"]),
            CancellationToken.None);

        Assert.Empty(_deliveries.Deliveries);
        Assert.Empty(_scheduler.Enqueued);
    }
}
