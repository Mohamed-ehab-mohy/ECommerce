using System.Security.Claims;
using ECommerce.UseCases.Common;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Common;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => User?.FindFirstValue("sub") is { } sub && Guid.TryParse(sub, out var id) ? id : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles => User?.FindAll("roles").Select(claim => claim.Value).ToList() ?? [];

    public IReadOnlyList<string> Permissions => User?.FindAll("perms").Select(claim => claim.Value).ToList() ?? [];
}
