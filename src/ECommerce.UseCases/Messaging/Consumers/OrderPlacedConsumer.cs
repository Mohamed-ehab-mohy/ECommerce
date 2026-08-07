using ECommerce.Domain.Events;
using ECommerce.UseCases.Messaging.Ports;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Messaging.Consumers;

public sealed class OrderPlacedConsumer(
    IInboxMessageRepository inbox,
    IOrderNotifier notifier,
    ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlaced>
{
    public const string QueueName = "order-events";

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var messageId = context.MessageId;

        if (messageId is null || messageId == Guid.Empty)
        {
            logger.LogWarning(
                "OrderPlaced {OrderId} arrived without a message id; processing without dedupe.",
                context.Message.OrderId);
            await notifier.NotifyPlacedAsync(context.Message, context.CancellationToken);
            return;
        }

        var firstDelivery = await inbox.TryConsumeAsync(
            QueueName,
            messageId.Value,
            context.CancellationToken);

        if (!firstDelivery)
        {
            logger.LogInformation(
                "Duplicate OrderPlaced message {MessageId} for order {OrderId} skipped.",
                messageId,
                context.Message.OrderId);
            return;
        }

        await notifier.NotifyPlacedAsync(context.Message, context.CancellationToken);
    }
}
