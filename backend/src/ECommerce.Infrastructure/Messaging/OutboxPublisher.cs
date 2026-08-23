using ECommerce.Domain.Abstractions;
using ECommerce.Infrastructure.Outbox;
using ECommerce.UseCases.Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Messaging;

public sealed class OutboxPublisher(
    IServiceProvider serviceProvider,
    IEventDispatcher dispatcher,
    OutboxMetrics metrics,
    ILogger<OutboxPublisher> logger)
{
    public async Task PublishAsync(
        OutboxMessage message,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        metrics.RecordLag(DateTime.UtcNow - message.OccurredOn);

        await dispatcher.DispatchAsync(domainEvent, cancellationToken);

        var publishEndpoint = serviceProvider.GetService<IPublishEndpoint>();
        if (publishEndpoint is null)
        {
            logger.LogDebug(
                "Message bus not configured; skipping publish for {EventType}.",
                message.EventType);
            return;
        }

        await publishEndpoint.Publish(
            domainEvent,
            domainEvent.GetType(),
            publishContext => publishContext.MessageId = message.Id,
            cancellationToken);

        metrics.RecordPublished();
    }
}
