using Hangfire.Dashboard;

namespace ECommerce.API.Jobs;

public sealed class OpsRoleDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private static readonly string[] OpsRoles = ["Admin", "SuperAdmin"];

    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        if (user.Identity is not { IsAuthenticated: true })
        {
            return false;
        }

        var roles = user.FindAll("roles").Select(claim => claim.Value);
        return roles.Any(role => OpsRoles.Contains(role));
    }
}
