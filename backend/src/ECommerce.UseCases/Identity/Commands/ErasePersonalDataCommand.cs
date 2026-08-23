
namespace ECommerce.UseCases.Identity.Commands;

public sealed record ErasePersonalDataCommand(Guid UserId) : IRequest<Result>;
