using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Notifications.Responses;
using MediatR;

namespace ECommerce.UseCases.Notifications.Queries;

public sealed record ListNotificationPreferencesQuery(Guid CustomerId)
    : IRequest<Result<IReadOnlyList<NotificationPreferenceResponse>>>;
