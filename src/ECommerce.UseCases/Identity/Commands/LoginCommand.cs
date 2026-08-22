using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record LoginCommand(
    string Email,
    string Password,
    string DeviceId,
    string IpAddress,
    string? GuestCartKey = null) : IRequest<Result<LoginResult>>;
