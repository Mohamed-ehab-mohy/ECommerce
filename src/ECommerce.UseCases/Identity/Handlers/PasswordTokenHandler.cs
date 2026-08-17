using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;
using MediatR;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class PasswordTokenHandler(
    IOAuthClientValidator clientValidator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider) : IRequestHandler<PasswordTokenCommand, Result<OAuthTokenResult>>
{
    public async Task<Result<OAuthTokenResult>> Handle(PasswordTokenCommand request, CancellationToken cancellationToken)
    {
        var validation = await clientValidator.ValidateAsync(request.ClientId, request.ClientSecret, cancellationToken);
        if (validation is null || !validation.IsValid)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidClient);
        }

        if (!validation.AllowedGrantTypes.Contains("password"))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.UnauthorizedGrantType);
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidGrant);
        }

        var customer = await users.GetByEmailAsync(request.Username.Trim().ToLowerInvariant(), cancellationToken);
        if (customer is null || !passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidCredentials);
        }

        if (!customer.EmailVerified)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidGrant);
        }

        var requestedScopes = ParseScopes(request.Scope);
        var allowedScopes = requestedScopes
            .Where(s => validation.AllowedScopes.Contains(s))
            .ToList();

        if (allowedScopes.Count == 0)
        {
            return Result<OAuthTokenResult>.Failure(OAuthErrors.InvalidScope);
        }

        var roles = await users.GetRolesAsync(customer.Id, cancellationToken);
        var permissions = await users.GetPermissionsAsync(customer.Id, cancellationToken);

        var claims = new AccessTokenClaims(
            customer.Id,
            customer.Email,
            roles,
            permissions,
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
