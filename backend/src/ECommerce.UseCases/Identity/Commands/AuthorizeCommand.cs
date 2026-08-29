
namespace ECommerce.UseCases.Identity.Commands;

public sealed record AuthorizeResult(
    string Code,
    string RedirectUri,
    string? Scope);

public sealed record AuthorizeCommand(
    Guid UserId,
    string ClientId,
    string RedirectUri,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Scope) : IRequest<Result<AuthorizeResult>>;
