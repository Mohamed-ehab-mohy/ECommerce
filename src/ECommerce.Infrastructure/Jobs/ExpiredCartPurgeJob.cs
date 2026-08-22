using ECommerce.Infrastructure.Data;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Jobs;

public sealed class ExpiredCartPurgeJob(
    ECommerceDbContext dbContext,
    ILogger<ExpiredCartPurgeJob> logger)
{
    public const string Schedule = "0 0 */6 * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var purged = await dbContext.Carts
            .Where(cart => cart.ExpiresAt < utcNow && !cart.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(cart => cart.IsDeleted, true),
                cancellationToken);

        logger.LogInformation("Purged {Count} expired carts at {Timestamp:O}.", purged, utcNow);
    }
}
