using ECommerce.Domain.Notifications;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Notifications.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Notifications;

public sealed class NotificationPreferenceRepository(ECommerceDbContext dbContext) : INotificationPreferenceRepository
{
    public Task<bool> IsEnabledAsync(
        Guid customerId,
        NotificationChannel channel,
        NotificationKind kind,
        CancellationToken cancellationToken) =>
        dbContext.Set<NotificationPreference>()
            .Where(preference => preference.CustomerId == customerId
                && preference.Channel == channel
                && preference.Kind == kind)
            .Select(preference => (bool?)preference.Enabled)
            .SingleOrDefaultAsync(cancellationToken)
            .ContinueWith(
                task => task.Result ?? true,
                cancellationToken);

    public Task<NotificationPreference?> GetAsync(
        Guid customerId,
        NotificationChannel channel,
        NotificationKind kind,
        CancellationToken cancellationToken) =>
        dbContext.Set<NotificationPreference>()
            .SingleOrDefaultAsync(
                preference => preference.CustomerId == customerId
                    && preference.Channel == channel
                    && preference.Kind == kind,
                cancellationToken);

    public Task<IReadOnlyList<NotificationPreference>> ListByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken) =>
        dbContext.Set<NotificationPreference>()
            .Where(preference => preference.CustomerId == customerId)
            .OrderBy(preference => preference.Channel)
            .ThenBy(preference => preference.Kind)
            .ToListAsync(cancellationToken)
            .ContinueWith(
                task => (IReadOnlyList<NotificationPreference>)task.Result,
                cancellationToken);

    public void Add(NotificationPreference preference) => dbContext.Set<NotificationPreference>().Add(preference);
}
