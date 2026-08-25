using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Domain.Tenants;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Tenants.Jobs;

public sealed class CheckTrialExpirationsJob(
    ECommerceDbContext dbContext,
    ILogger<CheckTrialExpirationsJob> logger)
{
    public const string Schedule = "0 0 * * *"; // Run daily at midnight

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running CheckTrialExpirationsJob at {Time}", DateTime.UtcNow);

        var expiredTrials = await dbContext.TenantSubscriptions
            .Include(ts => ts.Plan)
            .IgnoreQueryFilters()
            .Where(ts => ts.Status == SubscriptionStatus.Trial && ts.CurrentPeriodEnd < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (!expiredTrials.Any())
        {
            logger.LogInformation("No expired trials found.");
            return;
        }

        foreach (var subscription in expiredTrials)
        {
            subscription.Cancel();
            
            var tenant = await dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == subscription.TenantId, cancellationToken);
            if (tenant != null)
            {
                tenant.Suspend();
                logger.LogInformation("Suspended Tenant {TenantId} due to trial expiration.", tenant.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("CheckTrialExpirationsJob completed. Suspended {Count} tenants.", expiredTrials.Count);
    }
}