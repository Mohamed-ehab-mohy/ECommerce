using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
