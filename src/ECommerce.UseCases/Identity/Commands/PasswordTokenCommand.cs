using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record PasswordTokenCommand(
    string ClientId,
    string ClientSecret,
    string Username,
    string Password,
    string? Scope) : IRequest<Result<OAuthTokenResult>>;
