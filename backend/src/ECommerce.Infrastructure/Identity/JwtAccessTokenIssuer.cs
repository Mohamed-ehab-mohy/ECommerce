using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Identity;

public sealed class JwtAccessTokenIssuer(JwtOptions options, JwtRsaKeyProvider keyProvider, TimeProvider timeProvider) : IAccessTokenIssuer
{
    private static readonly JwtSecurityTokenHandler Handler = new();

    public IssuedAccessToken Issue(AccessTokenClaims claims)
    {
        var now = timeProvider.GetUtcNow();

        var jwtClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new(JwtRegisteredClaimNames.Jti, claims.TokenId)
        };

        jwtClaims.AddRange(claims.Roles.Select(role => new Claim("roles", role)));
        jwtClaims.AddRange(claims.Permissions.Select(permission => new Claim("perms", permission)));

        var credentials = new SigningCredentials(
            new RsaSecurityKey(keyProvider.Key),
            SecurityAlgorithms.RsaSha256);

        var expires = now.AddMinutes(options.AccessTokenTtlMinutes);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: jwtClaims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedAccessToken(Handler.WriteToken(token), expires);
    }
}
