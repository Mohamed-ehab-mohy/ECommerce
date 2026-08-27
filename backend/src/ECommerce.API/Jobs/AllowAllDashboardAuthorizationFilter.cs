using Hangfire.Dashboard;

namespace ECommerce.API.Jobs;

public sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
