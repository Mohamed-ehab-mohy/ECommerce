using ECommerce.UseCases.Notifications.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Notifications;

public sealed class NotificationSender(
    IEnumerable<INotificationProvider> providers,
    ILogger<NotificationSender> logger)
{
    public async Task SendAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var matched = providers
            .Where(provider => provider.Channel == envelope.Channel)
            .ToList();

        if (matched.Count == 0)
        {
            logger.LogWarning(
                "No notification provider registered for channel {Channel} (reference {ReferenceId}).",
                envelope.Channel,
                envelope.ReferenceId);
            return;
        }

        foreach (var provider in matched)
        {
            await provider.SendAsync(envelope, cancellationToken);
        }
    }
}
