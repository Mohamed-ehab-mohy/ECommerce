using ECommerce.UseCases.Promotions.Services;
using Hangfire;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>Recurring job that enforces promotion schedules (activation/pause) (US-E-007).</summary>
[AutomaticRetry(Attempts = 5)]
public sealed class PromotionScheduleEnforcerJob(PromotionScheduleEnforcer enforcer)
{
    public const string Schedule = "*/1 * * * *";

    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        enforcer.EnforceAsync(cancellationToken);
}
