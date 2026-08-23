
namespace ECommerce.UseCases.Identity.Commands;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    string Locale,
    string Currency) : IRequest<Result<Guid>>;
