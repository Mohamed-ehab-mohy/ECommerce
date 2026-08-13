using ECommerce.Domain.Payments;
using ECommerce.UseCases.Payments.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public sealed class ReconciliationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly ReconciliationService _service;

    public ReconciliationServiceTests()
    {
        _service = new ReconciliationService(
            _payments,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            NullLogger<ReconciliationService>.Instance);
    }

    private Payment AuthorizedPayment(string providerReference = "pi_1")
    {
        var payment = Payment.Create(null, "mock", "tok_1", "ct_1", providerReference, "USD", 39.90m, null, UtcNow);
        payment.MarkAuthorized(providerReference, UtcNow);
        _payments.Add(payment);
        return payment;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Snapshot_Creates_Pending_Record_For_Unreconciled_Payment()
    {
        var payment = AuthorizedPayment();

        var created = await _service.SnapshotPendingAsync(CancellationToken.None);

        Assert.Equal(1, created);
        var record = Assert.Single(_payments.ReconciliationRecords);
        Assert.Equal(payment.Id, record.PaymentId);
        Assert.Equal("mock", record.ProviderKey);
        Assert.Equal("pi_1", record.ProviderReference);
        Assert.Equal(ReconciliationStatus.Pending, record.Status);
        Assert.Equal("Authorized", record.RecordedStatus);
    }

    [Fact]
    public async Task Snapshot_Skips_Payments_Already_Snapshotted()
    {
        var payment = AuthorizedPayment();
        _payments.AddReconciliationRecord(PaymentReconciliationRecord.Create(
            payment.Id, payment.ProviderKey, payment.ProviderReference!, payment.Amount, payment.Currency,
            payment.Status.ToString(), UtcNow));

        var created = await _service.SnapshotPendingAsync(CancellationToken.None);

        Assert.Equal(0, created);
    }

    [Fact]
    public async Task Snapshot_Ignores_Payments_Without_Provider_Reference()
    {
        var payment = Payment.Create(null, "mock", "tok_1", "ct_1", null, "USD", 39.90m, null, UtcNow);
        _payments.Add(payment);

        var created = await _service.SnapshotPendingAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(_payments.ReconciliationRecords);
    }

    [Fact]
    public async Task Drift_Is_Detectable_Through_Status_Flag()
    {
        var payment = AuthorizedPayment();
        var record = PaymentReconciliationRecord.Create(
            payment.Id, payment.ProviderKey, payment.ProviderReference!, payment.Amount, payment.Currency,
            payment.Status.ToString(), UtcNow);
        _payments.AddReconciliationRecord(record);

        record.MarkDrift("provider reports captured; platform recorded authorized", UtcNow);

        var drifting = await _payments.GetReconciliationRecordsAsync(ReconciliationStatus.Drift, CancellationToken.None);
        var pending = await _payments.GetReconciliationRecordsAsync(ReconciliationStatus.Pending, CancellationToken.None);

        Assert.Equal(record.Id, Assert.Single(drifting).Id);
        Assert.Empty(pending);
        Assert.Equal("provider reports captured; platform recorded authorized", record.Detail);
    }
}
