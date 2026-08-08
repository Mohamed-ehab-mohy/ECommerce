using ECommerce.Domain.Notifications;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Notifications.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Notifications;

public sealed class StubSmsProvider(ILogger<StubSmsProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public string Key => "stub";

    public Task SendAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Outbound SMS (stub): To {Recipient} BodyLength {BodyLength} Reference {ReferenceId}",
            PiiMasker.MaskPhone(envelope.Recipient),
            envelope.Body.Length,
            envelope.ReferenceId);

        return Task.CompletedTask;
    }
}
