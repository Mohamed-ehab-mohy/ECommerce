using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record CloseAccountCommand(Guid UserId) : IRequest<Result>;
