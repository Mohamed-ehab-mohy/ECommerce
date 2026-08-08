using ECommerce.Domain.Notifications;

namespace ECommerce.UseCases.Notifications.Ports;

public interface INotificationQueue
{
    Task EnqueueAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);
}

public sealed record NotificationRequest(
    Guid? CustomerId,
    NotificationChannel Channel,
    NotificationKind Kind,
    string TemplateKey,
    string Locale,
    string Recipient,
    string ReferenceId,
    IReadOnlyDictionary<string, string> Placeholders,
    bool Transactional = false);
