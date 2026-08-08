using ECommerce.Infrastructure.Notifications;
using ECommerce.UseCases.Notifications.Ports;
using Hangfire;

namespace ECommerce.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 5)]
public sealed class SendNotificationJob(NotificationSender sender)
{
    public Task ExecuteAsync(NotificationEnvelope envelope)
    {
        return sender.SendAsync(envelope, CancellationToken.None);
    }
}
