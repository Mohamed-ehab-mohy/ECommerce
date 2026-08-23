using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Jobs;
using ECommerce.UseCases.Payments.Ports;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Payments;

/// <summary>Enqueues a failed refund into Hangfire for the retry job (UC-I-004).</summary>
public sealed class HangfireRefundRetryJobScheduler(
    IBackgroundJobClient? backgroundJobClient,
    ILogger<HangfireRefundRetryJobScheduler> logger) : IRefundRetryJobScheduler
{
    public void EnqueueRetry(Guid refundId)
    {
        if (backgroundJobClient is null)
        {
            logger.LogInformation("Hangfire not configured; refund {RefundId} will not be scheduled for retry.", refundId);
            return;
        }

        backgroundJobClient.Enqueue<RetryFailedRefundsJob>(job => job.ExecuteAsync(refundId, CancellationToken.None));
    }
}
