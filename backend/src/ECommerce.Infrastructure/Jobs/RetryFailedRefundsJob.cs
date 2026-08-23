using ECommerce.Domain.Payments;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Ports;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Jobs;

/// <summary>
/// Retries a failed refund execution through the originating provider (UC-I-004). The refund aggregate
/// enforces the attempt budget; the execute handler is idempotent by refund id (QAS-04).
/// </summary>
[AutomaticRetry(Attempts = 0)]
public sealed class RetryFailedRefundsJob(
    IRefundRepository refunds,
    ISender sender,
    ILogger<RetryFailedRefundsJob> logger)
{
    public async Task ExecuteAsync(Guid refundId, CancellationToken cancellationToken)
    {
        var refund = await refunds.GetByIdAsync(refundId, cancellationToken);
        if (refund is null || refund.Status != RefundStatus.Failed)
        {
            return;
        }

        var result = await sender.Send(new ExecuteRefundCommand(refundId), cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Refund retry for {RefundId} failed: {Error} (attempt {Attempt}).",
                refundId,
                result.Error.Description,
                refund.Attempts);
        }
    }
}
