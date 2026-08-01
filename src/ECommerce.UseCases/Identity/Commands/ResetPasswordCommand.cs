using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
