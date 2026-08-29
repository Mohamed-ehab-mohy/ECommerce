using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class AuthorizeCommandHandler(
    IOAuthClientValidator clientValidator,
    IAuthorizationCodeStore codeStore,
    IUserRepository users) : IRequestHandler<AuthorizeCommand, Result<AuthorizeResult>>
{
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(5);

    public async Task<Result<AuthorizeResult>> Handle(AuthorizeCommand request, CancellationToken cancellationToken)
    {
        var client = await clientValidator.GetClientAsync(request.ClientId, cancellationToken);
        if (client is null || !client.IsValid)
        {
            return Result<AuthorizeResult>.Failure(OAuthErrors.InvalidClient);
        }

        if (!client.AllowedGrantTypes.Contains("authorization_code"))
        {
            return Result<AuthorizeResult>.Failure(OAuthErrors.UnauthorizedGrantType);
        }

        if (!client.RedirectUris.Contains(request.RedirectUri, StringComparer.Ordinal))
        {
            return Result<AuthorizeResult>.Failure(OAuthErrors.InvalidRedirectUri);
        }

        if (!string.IsNullOrWhiteSpace(request.CodeChallenge))
        {
            if (!string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal))
            {
                return Result<AuthorizeResult>.Failure(OAuthErrors.InvalidCodeChallenge);
            }
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AuthorizeResult>.Failure(OAuthErrors.InvalidGrant);
        }

        var requestedScopes = ParseScopes(request.Scope);
        var allowedScopes = requestedScopes
            .Where(s => client.AllowedScopes.Contains(s))
            .ToList();

        if (allowedScopes.Count == 0)
        {
            return Result<AuthorizeResult>.Failure(OAuthErrors.InvalidScope);
        }

        var record = new AuthorizationCodeRecord(
            Code: string.Empty,
            UserId: request.UserId,
            ClientId: request.ClientId,
            RedirectUri: request.RedirectUri,
            Scopes: allowedScopes,
            CodeChallenge: string.IsNullOrWhiteSpace(request.CodeChallenge) ? null : request.CodeChallenge,
            CodeChallengeMethod: string.IsNullOrWhiteSpace(request.CodeChallenge) ? null : request.CodeChallengeMethod);

        var code = await codeStore.CreateAsync(record, CodeTtl, cancellationToken);

        return Result<AuthorizeResult>.Success(new AuthorizeResult(
            code,
            request.RedirectUri,
            string.Join(' ', allowedScopes)));
    }

    private static List<string> ParseScopes(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
