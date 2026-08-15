using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Responses;

/// <summary>A single drift flagged by a reconciliation run (T-DAT-015).</summary>
public sealed record ReconciliationDriftResponse(
    Guid RecordId,
    Guid PaymentId,
    string ProviderReference,
    ReconciliationStatus Status,
    string Detail);

/// <summary>Summary of a reconciliation run (provider vs platform) (US-I-005, US-I-007, T-DAT-015).</summary>
public sealed record ReconciliationRunResponse(
    int MatchedCount,
    int DriftCount,
    int UnmatchedCount,
    int ProviderOnlyCount,
    IReadOnlyList<ReconciliationDriftResponse> Drifts,
    DateTime CheckedAtUtc)
{
    public bool HasDrift => DriftCount > 0 || UnmatchedCount > 0 || ProviderOnlyCount > 0;
}
