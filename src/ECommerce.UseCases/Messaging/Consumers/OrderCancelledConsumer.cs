using ECommerce.Domain.Events;
using ECommerce.UseCases.Messaging.Ports;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Messaging.Consumers;

public sealed class OrderCancelledConsumer(
    IInboxMessageRepository inbox,
    IOrderNotifier notifier,
    ILogger<OrderCancelledConsumer> logger) : IConsumer<OrderCancelled>
{
    public const string QueueName = OrderPlacedConsumer.QueueName;

    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        var messageId = context.MessageId;

        if (messageId is null || messageId == Guid.Empty)
        {
            logger.LogWarning(
                "OrderCancelled {OrderId} arrived without a message id; processing without dedupe.",
                context.Message.OrderId);
            await notifier.NotifyCancelledAsync(context.Message, context.CancellationToken);
            return;
        }

        var firstDelivery = await inbox.TryConsumeAsync(
            QueueName,
            messageId.Value,
            context.CancellationToken);

        if (!firstDelivery)
        {
            logger.LogInformation(
                "Duplicate OrderCancelled message {MessageId} for order {OrderId} skipped.",
                messageId,
                context.Message.OrderId);
            return;
        }

        await notifier.NotifyCancelledAsync(context.Message, context.CancellationToken);
    }
}
