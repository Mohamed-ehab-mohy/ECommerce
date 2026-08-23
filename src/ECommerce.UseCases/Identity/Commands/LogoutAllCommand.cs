
namespace ECommerce.UseCases.Identity.Commands;

public sealed record LogoutAllCommand(Guid UserId) : IRequest<Result>;
