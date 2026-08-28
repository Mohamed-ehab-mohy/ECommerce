using ECommerce.Domain.Audit;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Payments.Ports;
using ECommerce.UseCases.Payments.Responses;
using Microsoft.Extensions.Logging;

namespace ECommerce.UseCases.Payments.Services;

/// <summary>
/// Snapshot provider-backed payments into the reconciliation ledger and runs the
/// nightly reconciliation feed: provider transactions are compared against the platform ledger,
/// matching records are marked Matched, mismatches are flagged as Drift, and provider-side
/// transactions without a platform record are surfaced.
/// </summary>
public sealed class ReconciliationService(
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IPaymentProviderFactory providerFactory,
    IAuditLogWriter auditLogWriter,
    ILogger<ReconciliationService> logger)
{
    private static readonly TimeSpan Lookback = TimeSpan.FromDays(7);

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

    /// <summary>
    /// Compares pending reconciliation records against the provider statement and flags Matched /
    /// Drift / Unmatched. Detects provider-side transactions with no platform record. Returns the run report.
    /// </summary>
    public async Task<ReconciliationRunResponse> RunAsync(CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // A run always snapshots unreconciled payments first so the feed is complete.
        await SnapshotPendingAsync(cancellationToken);

        var pending = await payments.GetReconciliationRecordsAsync(ReconciliationStatus.Pending, cancellationToken);

        var report = pending.Count == 0
            ? new ReconciliationRunResponse(0, 0, 0, 0, [], utcNow)
            : await ReconcileAsync(pending, utcNow, cancellationToken);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.ReconciliationRun,
            "Reconciliation",
            After: new
            {
                Matched = report.MatchedCount,
                Drift = report.DriftCount,
                Unmatched = report.UnmatchedCount,
                ProviderOnly = report.ProviderOnlyCount,
                report.CheckedAtUtc
            }),
            cancellationToken);

        foreach (var drift in report.Drifts)
        {
            await auditLogWriter.WriteAsync(new AuditOperation(
                AuditActions.ReconciliationDrift,
                "Reconciliation",
                drift.RecordId.ToString(),
                After: new
                {
                    drift.PaymentId,
                    drift.ProviderReference,
                    drift.Status,
                    drift.Detail
                }),
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reconciliation run at {Timestamp:O}: {Matched} matched, {Drift} drift, {Unmatched} unmatched, {ProviderOnly} provider-only.",
            utcNow,
            report.MatchedCount,
            report.DriftCount,
            report.UnmatchedCount,
            report.ProviderOnlyCount);

        return report;
    }

    private async Task<ReconciliationRunResponse> ReconcileAsync(
        IReadOnlyList<PaymentReconciliationRecord> pending,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var fromUtc = utcNow.Subtract(Lookback);
        var drifts = new List<ReconciliationDriftResponse>();
        var matched = 0;
        var drift = 0;
        var unmatched = 0;
        var providerOnly = 0;

        var platformReferences = pending.Select(record => record.ProviderReference).ToHashSet();

        foreach (var group in pending.GroupBy(record => record.ProviderKey))
        {
            var provider = await TryGetProviderAsync(group.Key, cancellationToken);
            if (provider is null)
            {
                foreach (var record in group)
                {
                    record.MarkDrift($"provider {group.Key} unavailable for reconciliation", utcNow);
                    drift++;
                    drifts.Add(new ReconciliationDriftResponse(
                        record.Id,
                        record.PaymentId,
                        record.ProviderReference,
                        ReconciliationStatus.Drift,
                        record.Detail));
                }

                continue;
            }

            IReadOnlyList<ProviderTransaction> transactions;
            try
            {
                transactions = await provider.ListTransactionsAsync(fromUtc, utcNow, cancellationToken);
            }
            catch (Exception)
            {
                foreach (var record in group)
                {
                    record.MarkDrift($"provider {group.Key} failed to list transactions", utcNow);
                    drift++;
                    drifts.Add(new ReconciliationDriftResponse(
                        record.Id,
                        record.PaymentId,
                        record.ProviderReference,
                        ReconciliationStatus.Drift,
                        record.Detail));
                }

                continue;
            }

            var byReference = transactions
                .GroupBy(transaction => transaction.ProviderReference)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var transaction in transactions)
            {
                if (!platformReferences.Contains(transaction.ProviderReference))
                {
                    providerOnly++;
                }
            }

            foreach (var record in group)
            {
                if (!byReference.TryGetValue(record.ProviderReference, out var transaction))
                {
                    record.MarkUnmatched($"no provider transaction for {record.ProviderReference} in window", utcNow);
                    unmatched++;
                    continue;
                }

                if (IsSucceeded(transaction.Status) && AmountsEqual(transaction.Amount, record.Amount))
                {
                    record.MarkMatched(transaction.Status, utcNow);
                    matched++;
                    continue;
                }

                var detail =
                    $"provider reports {transaction.EventType}/{transaction.Status} {transaction.Amount} {transaction.Currency}; " +
                    $"platform recorded {record.RecordedStatus}/{record.Amount} {record.Currency}";
                record.MarkDrift(detail, utcNow);
                drift++;
                drifts.Add(new ReconciliationDriftResponse(
                    record.Id,
                    record.PaymentId,
                    record.ProviderReference,
                    ReconciliationStatus.Drift,
                    detail));
            }
        }

        return new ReconciliationRunResponse(matched, drift, unmatched, providerOnly, drifts, utcNow);
    }

    private async Task<IPaymentProvider?> TryGetProviderAsync(string providerKey, CancellationToken cancellationToken)
    {
        try
        {
            return await providerFactory.GetAsync(providerKey, cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsSucceeded(string status) =>
        status.Equals("succeeded", StringComparison.OrdinalIgnoreCase)
        || status.Equals("captured", StringComparison.OrdinalIgnoreCase);

    private static bool AmountsEqual(decimal left, decimal right) => Math.Abs(left - right) <= 0.005m;
}
