
namespace ECommerce.UseCases.Identity.Commands;

public sealed record VerifyEmailCommand(string Token) : IRequest<Result>;
