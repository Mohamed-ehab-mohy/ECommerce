using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
