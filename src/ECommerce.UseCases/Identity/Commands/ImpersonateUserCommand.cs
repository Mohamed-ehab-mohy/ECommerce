
namespace ECommerce.UseCases.Identity.Commands;

public sealed record ImpersonateUserCommand(Guid TargetUserId) : IRequest<Result<ImpersonateResult>>;
