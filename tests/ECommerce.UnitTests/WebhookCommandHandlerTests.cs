using ECommerce.Domain.Integrations;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Integrations.Commands;
using ECommerce.UseCases.Integrations.Handlers;
using ECommerce.UseCases.Integrations.Queries;

namespace ECommerce.UnitTests;

public sealed class WebhookCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeWebhookEndpointRepository _endpoints = new();

    private readonly FakeWebhookDeliveryRepository _deliveries = new();

    private readonly FakeWebhookDeliveryJobScheduler _scheduler = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private CreateWebhookEndpointCommandHandler CreateHandler() =>
        new(
            _endpoints,
            _unitOfWork,
            new CreateWebhookEndpointCommandValidator(),
            new FixedTimeProvider(UtcNow));

    private RotateWebhookSecretCommandHandler CreateRotateHandler() =>
        new(
            _endpoints,
            _unitOfWork,
            new RotateWebhookSecretCommandValidator(),
            new FixedTimeProvider(UtcNow));

    private ReplayWebhookCommandHandler CreateReplayHandler() =>
        new(
            _endpoints,
            _deliveries,
            _scheduler,
            _unitOfWork,
            new ReplayWebhookCommandValidator(),
            new FixedTimeProvider(UtcNow));

    private WebhookEndpoint AddEndpoint()
    {
        var endpoint = WebhookEndpoint.Create(
            "Partner",
            "https://partner.test/hook",
            "old-secret",
            [WebhookEventTypes.OrderPlaced],
            UtcNow);
        _endpoints.Add(endpoint);
        return endpoint;
    }

    [Fact]
    public async Task Create_Registers_Endpoint_And_Returns_Secret_Once()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateWebhookEndpointCommand(
                "Partner",
                "https://partner.test/hook",
                [WebhookEventTypes.OrderPlaced, WebhookEventTypes.OrderShipped]),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var endpoint = Assert.Single(_endpoints.Endpoints);
        Assert.Equal(result.Value.EndpointId, endpoint.Id);
        Assert.Equal(WebhookEventTypes.OrderPlaced, endpoint.EventTypes.First());
        Assert.Equal(WebhookEventTypes.OrderShipped, endpoint.EventTypes.Skip(1).Single());
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Secret));
        Assert.Equal(endpoint.Secret, result.Value.Secret);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_Rejects_Unsupported_Event_Type()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateWebhookEndpointCommand("Partner", "https://partner.test/hook", ["order.unknown"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_endpoints.Endpoints);
    }

    [Fact]
    public async Task Rotate_Returns_New_Secret_And_Persists()
    {
        var endpoint = AddEndpoint();
        var handler = CreateRotateHandler();

        var result = await handler.Handle(new RotateWebhookSecretCommand(endpoint.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.NotEqual("old-secret", result.Value.Secret);
        Assert.Equal(result.Value.Secret, endpoint.Secret);
        Assert.Equal(UtcNow, endpoint.SecretRotatedAtUtc);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Rotate_Unknown_Endpoint_Fails()
    {
        var handler = CreateRotateHandler();

        var result = await handler.Handle(new RotateWebhookSecretCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WebhookErrors.EndpointNotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Replay_Single_Delivery_Resets_And_Enqueues()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        delivery.RecordFailure(500, "boom", null, UtcNow);
        _deliveries.Add(delivery);
        var handler = CreateReplayHandler();

        var result = await handler.Handle(
            new ReplayWebhookCommand(endpoint.Id, delivery.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(1, result.Value.Replayed);
        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.Attempts);
        Assert.Equal(delivery.Id, Assert.Single(_scheduler.Enqueued));
    }

    [Fact]
    public async Task Replay_Already_Delivered_Is_Left_Untouched()
    {
        var endpoint = AddEndpoint();
        var delivery = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        delivery.RecordSuccess(200, UtcNow);
        _deliveries.Add(delivery);
        var handler = CreateReplayHandler();

        var result = await handler.Handle(
            new ReplayWebhookCommand(endpoint.Id, delivery.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(WebhookDeliveryStatus.Delivered, delivery.Status);
        Assert.Empty(_scheduler.Enqueued);
    }

    [Fact]
    public async Task Replay_Unknown_Delivery_Fails()
    {
        var endpoint = AddEndpoint();
        var handler = CreateReplayHandler();

        var result = await handler.Handle(
            new ReplayWebhookCommand(endpoint.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WebhookErrors.DeliveryNotFound, result.Error);
    }

    [Fact]
    public async Task Replay_All_Resets_Failed_And_Suspended_Deliveries()
    {
        var endpoint = AddEndpoint();
        var failed = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        failed.RecordFailure(500, "boom", null, UtcNow);
        var suspended = WebhookDelivery.Create(endpoint.Id, "evt_2", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        suspended.Suspend("gave up", UtcNow);
        var delivered = WebhookDelivery.Create(endpoint.Id, "evt_3", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        delivered.RecordSuccess(200, UtcNow);
        _deliveries.Add(failed);
        _deliveries.Add(suspended);
        _deliveries.Add(delivered);
        var handler = CreateReplayHandler();

        var result = await handler.Handle(new ReplayWebhookCommand(endpoint.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value.Replayed);
        Assert.Equal(WebhookDeliveryStatus.Pending, failed.Status);
        Assert.Equal(WebhookDeliveryStatus.Pending, suspended.Status);
        Assert.Equal(WebhookDeliveryStatus.Delivered, delivered.Status);
        Assert.Equal(2, _scheduler.Enqueued.Count);
    }

    [Fact]
    public async Task List_Endpoints_Does_Not_Expose_Secrets()
    {
        _endpoints.Add(WebhookEndpoint.Create(
            "Partner",
            "https://partner.test/hook",
            "super-secret",
            [WebhookEventTypes.OrderPlaced],
            UtcNow));
        var handler = new ListWebhookEndpointsQueryHandler(_endpoints);

        var result = await handler.Handle(new ListWebhookEndpointsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var response = Assert.Single(result.Value);
        Assert.Equal("Partner", response.Name);
        Assert.True(response.IsActive);
        Assert.Equal([WebhookEventTypes.OrderPlaced], response.EventTypes);
    }

    [Fact]
    public async Task List_Deliveries_Returns_Log_For_Endpoint()
    {
        var endpoint = AddEndpoint();
        var first = WebhookDelivery.Create(endpoint.Id, "evt_1", WebhookEventTypes.OrderPlaced, "{}", UtcNow);
        var second = WebhookDelivery.Create(endpoint.Id, "evt_2", WebhookEventTypes.OrderShipped, "{}", UtcNow.AddMinutes(1));
        _deliveries.Add(first);
        _deliveries.Add(second);
        var handler = new ListWebhookDeliveriesQueryHandler(
            _endpoints,
            _deliveries,
            new ListWebhookDeliveriesQueryValidator());

        var result = await handler.Handle(
            new ListWebhookDeliveriesQuery(endpoint.Id, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(second.Id, result.Value[0].DeliveryId);
        Assert.Equal("Pending", result.Value[1].Status);
    }

    [Fact]
    public async Task List_Deliveries_Applies_Limit()
    {
        var endpoint = AddEndpoint();
        for (var index = 0; index < 5; index++)
        {
            _deliveries.Add(WebhookDelivery.Create(endpoint.Id, $"evt_{index}", WebhookEventTypes.OrderPlaced, "{}", UtcNow.AddMinutes(index)));
        }

        var handler = new ListWebhookDeliveriesQueryHandler(
            _endpoints,
            _deliveries,
            new ListWebhookDeliveriesQueryValidator());

        var result = await handler.Handle(
            new ListWebhookDeliveriesQuery(endpoint.Id, 3),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task List_Deliveries_Unknown_Endpoint_Fails()
    {
        var handler = new ListWebhookDeliveriesQueryHandler(
            _endpoints,
            _deliveries,
            new ListWebhookDeliveriesQueryValidator());

        var result = await handler.Handle(
            new ListWebhookDeliveriesQuery(Guid.NewGuid(), null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WebhookErrors.EndpointNotFound, result.Error);
    }

    [Fact]
    public void Commands_Require_Integrations_Permissions()
    {
        Assert.Equal(Permissions.IntegrationsWrite, new CreateWebhookEndpointCommand("P", "https://a.b/c", ["order.placed"]).Permission);
        Assert.Equal(Permissions.IntegrationsWrite, new RotateWebhookSecretCommand(Guid.NewGuid()).Permission);
        Assert.Equal(Permissions.IntegrationsWrite, new ReplayWebhookCommand(Guid.NewGuid(), null).Permission);
        Assert.Equal(Permissions.IntegrationsRead, new ListWebhookEndpointsQuery().Permission);
        Assert.Equal(Permissions.IntegrationsRead, new ListWebhookDeliveriesQuery(Guid.NewGuid(), null).Permission);
    }
}
