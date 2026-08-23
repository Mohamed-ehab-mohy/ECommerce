using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class ClientCredentialsTokenHandler(
    IOAuthClientValidator clientValidator,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider) : IRequestHandler<ClientCredentialsTokenCommand, Result<OAuthTokenResult>>
{
    public async Task<Result<OAuthTokenResult>> Handle(ClientCredentialsTokenCommand request, CancellationToken cancellationToken)
    {
        var validation = await clientValidator.ValidateAsync(request.ClientId, request.ClientSecret, cancellationToken);
        if (validation is null || !validation.IsValid)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidClient);
        }

        if (!validation.AllowedGrantTypes.Contains("client_credentials"))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.UnauthorizedGrantType);
        }

        var requestedScopes = ParseScopes(request.Scope);
        var allowedScopes = requestedScopes
            .Where(s => validation.AllowedScopes.Contains(s))
            .ToList();

        if (allowedScopes.Count == 0)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidScope);
        }

        var claims = new AccessTokenClaims(
            Guid.Empty,
            $"service:{request.ClientId}",
            [],
            allowedScopes,
            Guid.NewGuid().ToString());

        var issued = accessTokenIssuer.Issue(claims);
        var expiresInSeconds = (int)(issued.ExpiresAtUtc - timeProvider.GetUtcNow().UtcDateTime).TotalSeconds;

        return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
            issued.Value,
            "Bearer",
            expiresInSeconds,
            string.Join(' ', allowedScopes)));
    }

    private static List<string> ParseScopes(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
