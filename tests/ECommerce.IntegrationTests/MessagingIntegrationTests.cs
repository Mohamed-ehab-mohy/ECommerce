using System.Diagnostics.Metrics;
using ECommerce.Domain.Abstractions;
using ECommerce.Domain.Events;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Messaging;
using ECommerce.Infrastructure.Outbox;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Messaging.Consumers;
using ECommerce.UseCases.Messaging.Ports;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.IntegrationTests;

public sealed class MessagingIntegrationTests :
    IClassFixture<PostgresContainerFixture>,
    IClassFixture<RabbitMqContainerFixture>
{
    private readonly PostgresContainerFixture _postgres;

    private readonly RabbitMqContainerFixture _rabbitMq;

    public MessagingIntegrationTests(
        PostgresContainerFixture postgres,
        RabbitMqContainerFixture rabbitMq)
    {
        _postgres = postgres;
        _rabbitMq = rabbitMq;
    }

    [SkippableFact]
    public async Task Outbox_Publish_To_Bus_Consumer_Deduplicates_Inbox()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await using (var setup = CreateContext())
        {
            await setup.Database.MigrateAsync();
        }

        CountingInboxRepository.ResetCalls();
        var notifier = new CapturingNotifier();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ECommerceDbContext>(_ => CreateContext());
        services.AddScoped<IInboxMessageRepository, CountingInboxRepository>();
        services.AddScoped<IOrderNotifier>(_ => notifier);
        services.AddScoped<IEventDispatcher>(_ => new NoopEventDispatcher());
        services.AddSingleton(new OutboxMetrics(new FakeMeterFactory()));
        services.AddScoped<OutboxPublisher>();
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<OrderPlacedConsumer>();
            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(_rabbitMq.ConnectionString));

                cfg.ReceiveEndpoint(OrderPlacedConsumer.QueueName, endpoint =>
                {
                    endpoint.SetQuorumQueue();
                    endpoint.ConfigureConsumer<OrderPlacedConsumer>(context);
                });
            });
        });

        await using var provider = services.BuildServiceProvider();
        var busControl = provider.GetRequiredService<IBusControl>();
        await busControl.StartAsync();

        try
        {
            using var scope = provider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();

            var messageId = Guid.NewGuid();
            var orderPlaced = new OrderPlaced(
                Guid.NewGuid(),
                "E-20260807-000001",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ahmed@example.com",
                39.90m,
                "USD");
            var outboxMessage = new OutboxMessage
            {
                Id = messageId,
                AggregateId = orderPlaced.OrderId,
                EventType = orderPlaced.GetType().FullName!,
                Content = "{}",
                OccurredOn = DateTime.UtcNow
            };

            await publisher.PublishAsync(outboxMessage, orderPlaced, CancellationToken.None);
            await publisher.PublishAsync(outboxMessage, orderPlaced, CancellationToken.None);

            await WaitUntilAsync(() => CountingInboxRepository.TotalCalls >= 2);

            Assert.Single(notifier.Notified);

            await using var verify = CreateContext();
            Assert.Equal(1, await verify.InboxMessages.CountAsync(
                message => message.ConsumerQueue == OrderPlacedConsumer.QueueName && message.MessageId == messageId));
        }
        finally
        {
            await busControl.StopAsync();
        }
    }

    private ECommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(_postgres.ConnectionString)
            .Options;
        return new ECommerceDbContext(options);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail("Condition was not met within the timeout.");
    }

    private sealed class CountingInboxRepository(IServiceScopeFactory scopeFactory) : IInboxMessageRepository
    {
        private static long _calls;

        public static long TotalCalls => Interlocked.Read(ref _calls);

        public static void ResetCalls() => Interlocked.Exchange(ref _calls, 0);

        public async Task<bool> TryConsumeAsync(
            string consumerQueue,
            Guid messageId,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);

            using var scope = scopeFactory.CreateScope();
            var repository = new InboxMessageRepository(
                scope.ServiceProvider.GetRequiredService<ECommerceDbContext>());
            return await repository.TryConsumeAsync(consumerQueue, messageId, cancellationToken);
        }
    }

    private sealed class CapturingNotifier : IOrderNotifier
    {
        public List<OrderPlaced> Notified { get; } = [];

        public List<OrderCancelled> Cancelled { get; } = [];

        public Task NotifyPlacedAsync(OrderPlaced orderPlaced, CancellationToken cancellationToken)
        {
            Notified.Add(orderPlaced);
            return Task.CompletedTask;
        }

        public Task NotifyCancelledAsync(OrderCancelled orderCancelled, CancellationToken cancellationToken)
        {
            Cancelled.Add(orderCancelled);
            return Task.CompletedTask;
        }

        public Task NotifyShippedAsync(OrderShipped orderShipped, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoopEventDispatcher : IEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose() { }
    }
}
