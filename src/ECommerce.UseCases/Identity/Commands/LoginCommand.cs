using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record LoginCommand(
    string Email,
    string Password,
    string DeviceId,
    string IpAddress) : IRequest<Result<LoginResult>>;
