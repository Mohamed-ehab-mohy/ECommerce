using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Notifications.Commands;

public sealed record UpdateNotificationPreferenceCommand(
    Guid CustomerId,
    string Channel,
    string Kind,
    bool Enabled) : IRequest<Result>;
