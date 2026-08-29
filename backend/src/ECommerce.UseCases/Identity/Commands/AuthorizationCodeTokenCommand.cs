
namespace ECommerce.UseCases.Identity.Commands;

public sealed record AuthorizationCodeTokenCommand(
    string Code,
    string ClientId,
    string? ClientSecret,
    string RedirectUri,
    string? CodeVerifier,
    string? Scope) : IRequest<Result<OAuthTokenResult>>;
