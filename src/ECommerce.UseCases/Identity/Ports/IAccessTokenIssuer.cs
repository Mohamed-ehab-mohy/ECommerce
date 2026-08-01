namespace ECommerce.UseCases.Identity.Ports;

public sealed record AccessTokenClaims(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string TokenId);

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AccessTokenClaims claims);
}
