
namespace ECommerce.UseCases.Identity.Commands;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<LoginResult>>;
