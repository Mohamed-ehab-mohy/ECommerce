using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record VerifyMfaCommand(Guid CustomerId, string Code) : IRequest<Result>;
