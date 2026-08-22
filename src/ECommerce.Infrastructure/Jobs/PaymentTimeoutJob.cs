using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Data;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>
/// Safety-net job that fails payments stuck in Created or Authorized status
/// for longer than 1 hour, preventing indefinite holds.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class PaymentTimeoutJob(
    ECommerceDbContext dbContext,
    ILogger<PaymentTimeoutJob> logger)
{
    public const string Schedule = "*/15 * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var timeoutThreshold = utcNow.AddHours(-1);

        var timedOut = await dbContext.Set<Payment>()
            .Where(p =>
                (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.Authorized)
                && p.CreatedAt < timeoutThreshold
                && !p.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.Failed)
                    .SetProperty(p => p.UpdatedAt, utcNow),
                cancellationToken);

        if (timedOut > 0)
        {
            logger.LogInformation(
                "Failed {Count} timed-out payments at {Timestamp:O}.",
                timedOut,
                utcNow);
        }
    }
}
