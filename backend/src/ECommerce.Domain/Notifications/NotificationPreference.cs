using ECommerce.Domain.Common;

namespace ECommerce.Domain.Notifications;

public sealed class NotificationPreference : BaseEntity<Guid>
{
    private NotificationPreference()
    {
    }

    public Guid CustomerId { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public NotificationKind Kind { get; private set; }

    public bool Enabled { get; private set; }

    public static NotificationPreference Create(
        Guid customerId,
        NotificationChannel channel,
        NotificationKind kind,
        bool enabled,
        DateTime utcNow)
    {
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Channel = channel,
            Kind = kind,
            Enabled = enabled,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void SetEnabled(bool enabled, DateTime utcNow)
    {
        Enabled = enabled;
        UpdatedAt = utcNow;
    }
}
