using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
