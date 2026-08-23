using ECommerce.Domain.Notifications;
using ECommerce.UseCases.Notifications.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Notifications.Services;

public sealed class NotificationDispatcher(
    INotificationPreferenceRepository preferences,
    INotificationTemplateStore templateStore,
    INotificationQueue queue,
    ILogger<NotificationDispatcher> logger)
{
    public async Task DispatchAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        if (!request.Transactional && request.CustomerId is { } customerId)
        {
            var enabled = await preferences.IsEnabledAsync(
                customerId,
                request.Channel,
                request.Kind,
                cancellationToken);

            if (!enabled)
            {
                logger.LogInformation(
                    "Notification {Kind} for customer {CustomerId} suppressed by preference ({Channel}).",
                    request.Kind,
                    customerId,
                    request.Channel);
                return;
            }
        }

        var content = await templateStore.RenderAsync(
            request.TemplateKey,
            request.Locale,
            request.Placeholders,
            cancellationToken);

        await queue.EnqueueAsync(new NotificationEnvelope(
            request.Channel,
            request.Recipient,
            content.Subject,
            content.Body,
            request.Kind,
            request.ReferenceId), cancellationToken);

        logger.LogInformation(
            "Notification {Kind} queued for {Channel} reference {ReferenceId}.",
            request.Kind,
            request.Channel,
            request.ReferenceId);
    }
}
