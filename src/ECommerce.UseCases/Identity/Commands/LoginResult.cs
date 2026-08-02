using ECommerce.Domain.Identity;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles)
{
    public static LoginResult From(
        string accessToken,
        string refreshToken,
        int expiresInSeconds,
        Customer customer,
        IReadOnlyList<string> roles) =>
        new(accessToken, refreshToken, expiresInSeconds, customer.Id, customer.Email, roles);
}
