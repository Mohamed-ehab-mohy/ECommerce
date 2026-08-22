using ECommerce.Domain.Inventory;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Inventory.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>
/// Safety-net job that releases stock allocations (reservations) older than 30 minutes
/// back to available inventory when they were never fulfilled or released.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class StockReservationExpiryJob(
    ECommerceDbContext dbContext,
    IStockAllocator stockAllocator,
    ILogger<StockReservationExpiryJob> logger)
{
    public const string Schedule = "0 * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var expiryThreshold = utcNow.AddMinutes(-30);

        var latestAllocations = dbContext.Set<StockMovement>()
            .Where(m => m.Type == StockMovementType.Allocate && !m.IsDeleted)
            .GroupBy(m => m.StockItemId)
            .Select(g => new { StockItemId = g.Key, LatestAt = g.Max(m => m.CreatedAt) });

        var expiredItems = await (from s in dbContext.Set<StockItem>()
                                  join a in latestAllocations on s.Id equals a.StockItemId
                                  where s.Allocated > 0 && !s.IsDeleted && a.LatestAt < expiryThreshold
                                  select new AllocationRequestItem(s.Sku, s.Allocated))
            .ToListAsync(cancellationToken);

        if (expiredItems.Count == 0)
        {
            return;
        }

        await stockAllocator.ReleaseAsync(
            expiredItems,
            "reservation-expiry",
            "auto-expiry-job",
            utcNow,
            cancellationToken);

        logger.LogInformation(
            "Released {Count} expired stock reservations at {Timestamp:O}.",
            expiredItems.Count,
            utcNow);
    }
}
