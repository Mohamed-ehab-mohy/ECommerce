using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Notifications.Commands;

public sealed record UpdateNotificationPreferenceCommand(
    Guid CustomerId,
    string Channel,
    string Kind,
    bool Enabled) : IRequest<Result>;
