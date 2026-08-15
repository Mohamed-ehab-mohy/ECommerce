using ECommerce.Domain.Events;
using ECommerce.Domain.Payments;

namespace ECommerce.UnitTests;

public sealed class ReconciliationDriftEventTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

    private static PaymentReconciliationRecord CreateRecord() =>
        PaymentReconciliationRecord.Create(
            Guid.NewGuid(),
            "stripe",
            "pi_123",
            199.90m,
            "USD",
            "Captured",
            UtcNow);

    [Fact]
    public void MarkDrift_Raises_ReconciliationDriftDetected_With_Drift_Status()
    {
        var record = CreateRecord();

        record.MarkDrift("amount mismatch", UtcNow.AddHours(1));

        var drift = Assert.Single(record.DomainEvents.OfType<ReconciliationDriftDetected>());
        Assert.Equal(record.Id, drift.RecordId);
        Assert.Equal(record.PaymentId, drift.PaymentId);
        Assert.Equal("pi_123", drift.ProviderReference);
        Assert.Equal(199.90m, drift.Amount);
        Assert.Equal("USD", drift.Currency);
        Assert.Equal(ReconciliationStatus.Drift, drift.Status);
        Assert.Equal("amount mismatch", drift.Detail);
    }

    [Fact]
    public void MarkUnmatched_Raises_ReconciliationDriftDetected_With_Unmatched_Status()
    {
        var record = CreateRecord();

        record.MarkUnmatched("no platform record", UtcNow.AddHours(1));

        var drift = Assert.Single(record.DomainEvents.OfType<ReconciliationDriftDetected>());
        Assert.Equal(ReconciliationStatus.Unmatched, drift.Status);
        Assert.Equal("no platform record", drift.Detail);
    }

    [Fact]
    public void MarkMatched_Raises_No_Drift_Event()
    {
        var record = CreateRecord();

        record.MarkMatched("Captured", UtcNow.AddHours(1));

        Assert.Empty(record.DomainEvents.OfType<ReconciliationDriftDetected>());
    }
}
