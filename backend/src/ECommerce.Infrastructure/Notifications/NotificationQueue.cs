using ECommerce.UseCases.Notifications.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Notifications;

public sealed class NotificationQueue(
    IBackgroundJobClient? backgroundJobClient,
    NotificationSender sender,
    ILogger<NotificationQueue> logger) : INotificationQueue
{
    public Task EnqueueAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        if (backgroundJobClient is not null)
        {
            backgroundJobClient.Enqueue<Jobs.SendNotificationJob>(job => job.ExecuteAsync(envelope));
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Hangfire not configured; sending notification inline for {ReferenceId}.",
            envelope.ReferenceId);

        return sender.SendAsync(envelope, cancellationToken);
    }
}
