using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ErasePersonalDataCommand(Guid UserId) : IRequest<Result>;
