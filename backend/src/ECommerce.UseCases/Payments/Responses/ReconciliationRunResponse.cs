using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Responses;

/// <summary>A single drift flagged by a reconciliation run.</summary>
public sealed record ReconciliationDriftResponse(
    Guid RecordId,
    Guid PaymentId,
    string ProviderReference,
    ReconciliationStatus Status,
    string Detail);

/// <summary>Summary of a reconciliation run (provider vs platform).</summary>
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
