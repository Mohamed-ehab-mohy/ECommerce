using ECommerce.Domain.Payments;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Payments.Services;

/// <summary>
/// Snapshots provider-backed payments into the reconciliation ledger (T-DAT-010). The S12 nightly job
/// fills provider statuses and flags drift; this creates the Pending rows and detects unreconciled payments.
/// </summary>
public sealed class ReconciliationService(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ReconciliationService> logger)
{
    public async Task<int> SnapshotPendingAsync(CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var candidates = await payments.GetUnreconciledAsync(cancellationToken);

        foreach (var payment in candidates)
        {
            payments.AddReconciliationRecord(PaymentReconciliationRecord.Create(
                payment.Id,
                payment.ProviderKey,
                payment.ProviderReference!,
                payment.Amount,
                payment.Currency,
                payment.Status.ToString(),
                utcNow));
        }

        if (candidates.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Reconciliation snapshot created {Count} pending records at {Timestamp:O}.",
                candidates.Count,
                utcNow);
        }

        return candidates.Count;
    }
}
