using ECommerce.Domain.Notifications;

namespace ECommerce.UseCases.Notifications.Ports;

public sealed record NotificationEnvelope(
    NotificationChannel Channel,
    string Recipient,
    string Subject,
    string Body,
    NotificationKind Kind,
    string ReferenceId);

public interface INotificationProvider
{
    NotificationChannel Channel { get; }

    string Key { get; }

    Task SendAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);
}
