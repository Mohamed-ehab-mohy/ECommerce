using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
