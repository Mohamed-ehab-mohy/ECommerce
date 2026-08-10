using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Events;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Outbox;
using ECommerce.UseCases.Messaging.Consumers;
using ECommerce.UseCases.Messaging.Ports;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public sealed class OutboxPublisherTests
{
    private static OutboxMessage CreateOutboxMessage(OrderPlaced orderPlaced, Guid messageId) =>
        new()
        {
            Id = messageId,
            AggregateId = orderPlaced.OrderId,
            EventType = orderPlaced.GetType().FullName!,
            Content = "{}",
            OccurredOn = DateTime.UtcNow.AddSeconds(-3)
        };

    private static OrderPlaced CreateOrderPlaced() =>
        new(Guid.NewGuid(), "E-20260807-000001", Guid.NewGuid(), Guid.NewGuid(), "ahmed@example.com", 39.90m, "USD");

    private static OutboxMetrics CreateMetrics() => new(new FakeMeterFactory());

    [Fact]
    public async Task Publish_Dispatches_Locally_And_Publishes_With_Outbox_MessageId()
    {
        var dispatcher = new FakeEventDispatcher();
        var metrics = CreateMetrics();
        var inbox = new FakeInboxMessageRepository();
        var notifier = new CapturingOrderNotifier();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IInboxMessageRepository>(inbox);
        services.AddSingleton<IOrderNotifier>(notifier);
        services.AddSingleton(dispatcher);
        services.AddMassTransitTestHarness(config => config.AddConsumer<OrderPlacedConsumer>());

        var provider = services.BuildServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            using var scope = provider.CreateScope();
            var publisher = new OutboxPublisher(scope.ServiceProvider, dispatcher, metrics, NullLogger<OutboxPublisher>.Instance);
            var messageId = Guid.NewGuid();
            var orderPlaced = CreateOrderPlaced();

            await publisher.PublishAsync(CreateOutboxMessage(orderPlaced, messageId), orderPlaced, CancellationToken.None);

            Assert.True(await harness.Consumed.Any<OrderPlaced>());
            var consumed = await harness.Consumed.SelectAsync<OrderPlaced>().First();
            Assert.Equal(messageId, consumed.Context.MessageId);
            Assert.Single(dispatcher.Dispatched);
            Assert.Single(notifier.Notified);
        }
        finally
        {
            await harness.Stop();
            await provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task Publish_Without_MessageBus_Dispatches_Locally_And_Skips_Publish()
    {
        var dispatcher = new FakeEventDispatcher();
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var publisher = new OutboxPublisher(
            provider,
            dispatcher,
            CreateMetrics(),
            NullLogger<OutboxPublisher>.Instance);
        var orderPlaced = CreateOrderPlaced();

        await publisher.PublishAsync(CreateOutboxMessage(orderPlaced, Guid.NewGuid()), orderPlaced, CancellationToken.None);

        Assert.Single(dispatcher.Dispatched);
    }
}
