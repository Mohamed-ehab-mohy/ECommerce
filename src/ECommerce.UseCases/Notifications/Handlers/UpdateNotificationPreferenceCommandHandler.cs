using ECommerce.Domain.Audit;
using ECommerce.Domain.Notifications;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Notifications.Commands;
using ECommerce.UseCases.Notifications.Ports;
using ECommerce.UseCases.Notifications.Queries;
using ECommerce.UseCases.Notifications.Responses;

namespace ECommerce.UseCases.Notifications.Handlers;

public sealed class UpdateNotificationPreferenceCommandHandler(
    INotificationPreferenceRepository preferences,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateNotificationPreferenceCommand, Result>
{
    public async Task<Result> Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        if (!TryParse<NotificationChannel>(request.Channel, out var channel)
            || !TryParse<NotificationKind>(request.Kind, out var kind))
        {
            return Result.Failure(new Error(
                "Notifications.InvalidChannelOrKind",
                "Unknown notification channel or kind.",
                ErrorType.Validation));
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await preferences.GetAsync(
            request.CustomerId,
            channel,
            kind,
            cancellationToken);

        if (existing is null)
        {
            preferences.Add(NotificationPreference.Create(
                request.CustomerId,
                channel,
                kind,
                request.Enabled,
                utcNow));
        }
        else
        {
            existing.SetEnabled(request.Enabled, utcNow);
        }

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.NotificationPreferenceUpdated,
            "NotificationPreference",
            $"{request.CustomerId}:{channel}:{kind}",
            After: new { Channel = channel, Kind = kind, request.Enabled }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out result);
}

public sealed class ListNotificationPreferencesQueryHandler(
    INotificationPreferenceRepository preferences) : IRequestHandler<
    ListNotificationPreferencesQuery,
    Result<IReadOnlyList<NotificationPreferenceResponse>>>
{
    public async Task<Result<IReadOnlyList<NotificationPreferenceResponse>>> Handle(
        ListNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await preferences.ListByCustomerAsync(request.CustomerId, cancellationToken);

        var response = items
            .Select(preference => new NotificationPreferenceResponse(
                preference.Id,
                preference.Channel.ToString(),
                preference.Kind.ToString(),
                preference.Enabled))
            .ToList();

        return Result<IReadOnlyList<NotificationPreferenceResponse>>.Success(response);
    }
}
