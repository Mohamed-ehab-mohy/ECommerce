using ECommerce.Domain.Notifications;

namespace ECommerce.UseCases.Notifications.Ports;

public interface INotificationPreferenceRepository
{
    Task<bool> IsEnabledAsync(
        Guid customerId,
        NotificationChannel channel,
        NotificationKind kind,
        CancellationToken cancellationToken);

    Task<NotificationPreference?> GetAsync(
        Guid customerId,
        NotificationChannel channel,
        NotificationKind kind,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationPreference>> ListByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    void Add(NotificationPreference preference);
}
