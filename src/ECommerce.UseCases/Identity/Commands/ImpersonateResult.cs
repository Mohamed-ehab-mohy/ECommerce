using ECommerce.Domain.Identity;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record ImpersonateResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    Guid ImpersonatorId);
