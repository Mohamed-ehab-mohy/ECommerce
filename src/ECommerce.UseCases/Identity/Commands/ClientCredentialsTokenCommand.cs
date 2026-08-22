using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ClientCredentialsTokenCommand(
    string ClientId,
    string ClientSecret,
    string? Scope) : IRequest<Result<OAuthTokenResult>>;
