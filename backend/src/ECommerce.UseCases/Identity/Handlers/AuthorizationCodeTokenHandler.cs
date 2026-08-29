using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class AuthorizationCodeTokenHandler(
    IOAuthClientValidator clientValidator,
    IAuthorizationCodeStore codeStore,
    IUserRepository users,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider) : IRequestHandler<AuthorizationCodeTokenCommand, Result<OAuthTokenResult>>
{
    public async Task<Result<OAuthTokenResult>> Handle(AuthorizationCodeTokenCommand request, CancellationToken cancellationToken)
    {
        var client = await clientValidator.GetClientAsync(request.ClientId, cancellationToken);
        if (client is null || !client.IsValid)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidClient);
        }

        if (!client.AllowedGrantTypes.Contains("authorization_code"))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.UnauthorizedGrantType);
        }

        if (client.RequiresSecret)
        {
            var auth = await clientValidator.ValidateAsync(request.ClientId, request.ClientSecret ?? string.Empty, cancellationToken);
            if (auth is null || !auth.IsValid)
            {
                return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidClient);
            }
        }

        var code = await codeStore.ConsumeAsync(request.Code, cancellationToken);
        if (code is null)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidGrant);
        }

        if (!string.Equals(code.ClientId, request.ClientId, StringComparison.Ordinal)
            || !string.Equals(code.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidGrant);
        }

        if (code.CodeChallenge is { } challenge)
        {
            if (string.IsNullOrWhiteSpace(request.CodeVerifier)
                || !PkceHelper.Verify(request.CodeVerifier, challenge, code.CodeChallengeMethod ?? "S256"))
            {
                return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidPkceVerifier);
            }
        }

        var customer = await users.GetByIdAsync(code.UserId, cancellationToken);
        if (customer is null)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidGrant);
        }

        var roles = await users.GetRolesAsync(customer.Id, cancellationToken);
        var permissions = await users.GetPermissionsAsync(customer.Id, cancellationToken);

        var claims = new AccessTokenClaims(
            customer.Id,
            customer.Email,
            roles,
            permissions,
            Guid.NewGuid().ToString(),
            customer.TenantId);

        var issued = accessTokenIssuer.Issue(claims);
        var expiresInSeconds = (int)(issued.ExpiresAtUtc - timeProvider.GetUtcNow().UtcDateTime).TotalSeconds;

        return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
            issued.Value,
            "Bearer",
            expiresInSeconds,
            string.Join(' ', code.Scopes)));
    }
}
