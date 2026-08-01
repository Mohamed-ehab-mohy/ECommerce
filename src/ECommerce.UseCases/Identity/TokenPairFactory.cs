using ECommerce.Domain.Identity;
using ECommerce.UseCases.Identity.Commands;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.UseCases.Identity;

public sealed class TokenPairFactory(IAccessTokenIssuer accessTokenIssuer, AuthSettings settings, TimeProvider timeProvider)
{
    public IssuedPair Issue(Customer customer, string deviceId, Guid familyId)
    {
        var utcNow = timeProvider.GetUtcNow();

        var rawToken = RefreshTokens.Create();
        var refreshToken = RefreshToken.Create(
            customer.Id,
            familyId,
            deviceId,
            RefreshTokens.Hash(rawToken),
            utcNow.UtcDateTime.AddDays(settings.RefreshTokenTtlDays),
            utcNow.UtcDateTime);

        var issuedAccessToken = accessTokenIssuer.Issue(new AccessTokenClaims(
            customer.Id,
            customer.Email,
            [IdentityRoles.Customer],
            [],
            Guid.NewGuid().ToString()));

        var expiresInSeconds = (int)(issuedAccessToken.ExpiresAtUtc - utcNow).TotalSeconds;

        return new IssuedPair(
            refreshToken,
            LoginResult.From(issuedAccessToken.Value, rawToken, expiresInSeconds, customer));
    }
}

public sealed record IssuedPair(RefreshToken RefreshToken, LoginResult Result);
