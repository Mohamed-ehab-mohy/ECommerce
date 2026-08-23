using ECommerce.Domain.Events;
using ECommerce.UseCases.Messaging.Consumers;
using ECommerce.UseCases.Messaging.Ports;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.UnitTests;

public sealed class OrderPlacedConsumerTests
{
    private static OrderPlaced CreateOrderPlaced() =>
        new(Guid.NewGuid(), "E-20260807-000001", Guid.NewGuid(), Guid.NewGuid(), "ahmed@example.com", 39.90m, "USD");

    private static async Task<(ITestHarness Harness, ServiceProvider Provider)> StartAsync(
        FakeInboxMessageRepository inbox,
        CapturingOrderNotifier notifier)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IInboxMessageRepository>(inbox);
        services.AddSingleton<IOrderNotifier>(notifier);
        services.AddMassTransitTestHarness(config => config.AddConsumer<OrderPlacedConsumer>());

        var provider = services.BuildServiceProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return (harness, provider);
    }

    [Fact]
    public async Task Consume_FirstDelivery_Claims_Inbox_And_Notifies()
    {
        var inbox = new FakeInboxMessageRepository();
        var notifier = new CapturingOrderNotifier();
        var (harness, provider) = await StartAsync(inbox, notifier);

        try
        {
            var messageId = Guid.NewGuid();
            await harness.Bus.Publish(CreateOrderPlaced(), context => context.MessageId = messageId);

            Assert.True(await harness.Consumed.Any<OrderPlaced>());
            Assert.Single(notifier.Notified);
            Assert.Equal(1, inbox.ConsumeCalls);
        }
        finally
        {
            await harness.Stop();
            await provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task Consume_Duplicate_Message_Id_Skips_Notifier()
    {
        var inbox = new FakeInboxMessageRepository();
        var notifier = new CapturingOrderNotifier();
        var (harness, provider) = await StartAsync(inbox, notifier);

        try
        {
            var messageId = Guid.NewGuid();
            var orderPlaced = CreateOrderPlaced();

            await harness.Bus.Publish(orderPlaced, context => context.MessageId = messageId);
            Assert.True(await harness.Consumed.Any<OrderPlaced>());
            await harness.Bus.Publish(orderPlaced, context => context.MessageId = messageId);
            await WaitUntilAsync(() => inbox.ConsumeCalls == 2);

            Assert.Single(notifier.Notified);
            Assert.Equal(2, inbox.ConsumeCalls);
        }
        finally
        {
            await harness.Stop();
            await provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task Consume_Message_Without_Id_Processes_Without_Dedupe()
    {
        var inbox = new FakeInboxMessageRepository();
        var notifier = new CapturingOrderNotifier();
        var (harness, provider) = await StartAsync(inbox, notifier);

        try
        {
            await harness.Bus.Publish(CreateOrderPlaced(), context => context.MessageId = Guid.Empty);

            Assert.True(await harness.Consumed.Any<OrderPlaced>());
            Assert.Single(notifier.Notified);
            Assert.Equal(0, inbox.ConsumeCalls);
        }
        finally
        {
            await harness.Stop();
            await provider.DisposeAsync();
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail("Condition was not met within the timeout.");
    }
}
