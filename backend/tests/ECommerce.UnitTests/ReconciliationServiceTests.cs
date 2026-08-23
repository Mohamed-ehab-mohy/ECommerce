using ECommerce.Domain.Payments;
using ECommerce.UseCases.Payments.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public sealed class ReconciliationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakePaymentProviderFactory _providerFactory = new();

    private readonly FakeAuditLogWriter _audit = new();

    private readonly ReconciliationService _service;

    public ReconciliationServiceTests()
    {
        _service = new ReconciliationService(
            _payments,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            _providerFactory,
            _audit,
            NullLogger<ReconciliationService>.Instance);
    }

    private Payment AuthorizedPayment(string providerReference = "pi_1")
    {
        var payment = Payment.Create(null, "mock", "tok_1", "ct_1", providerReference, "USD", 39.90m, null, UtcNow);
        payment.MarkAuthorized(providerReference, UtcNow);
        _payments.Add(payment);
        return payment;
    }

    private static PaymentReconciliationRecord PendingRecord(Payment payment) =>
        PaymentReconciliationRecord.Create(
            payment.Id,
            payment.ProviderKey,
            payment.ProviderReference!,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.UpdatedAt);

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
        _payments.AddReconciliationRecord(PendingRecord(payment));

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
        var record = PendingRecord(payment);
        _payments.AddReconciliationRecord(record);

        record.MarkDrift("provider reports captured; platform recorded authorized", UtcNow);

        var drifting = await _payments.GetReconciliationRecordsAsync(ReconciliationStatus.Drift, CancellationToken.None);
        var pending = await _payments.GetReconciliationRecordsAsync(ReconciliationStatus.Pending, CancellationToken.None);

        Assert.Equal(record.Id, Assert.Single(drifting).Id);
        Assert.Empty(pending);
        Assert.Equal("provider reports captured; platform recorded authorized", record.Detail);
    }

    [Fact]
    public async Task Run_Marks_Matching_Record_As_Matched()
    {
        var payment = AuthorizedPayment();
        _payments.AddReconciliationRecord(PendingRecord(payment));
        _providerFactory.Provider.Transactions.Add(new ProviderTransaction(
            "pi_1", "authorized", 39.90m, "USD", "succeeded", UtcNow.AddMinutes(-5)));

        var report = await _service.RunAsync(CancellationToken.None);

        Assert.Equal(1, report.MatchedCount);
        Assert.Equal(0, report.DriftCount);
        Assert.False(report.HasDrift);
        var record = Assert.Single(_payments.ReconciliationRecords);
        Assert.Equal(ReconciliationStatus.Matched, record.Status);
        Assert.Equal("succeeded", record.ProviderStatus);
    }

    [Fact]
    public async Task Run_Flags_Amount_Mismatch_As_Drift()
    {
        var payment = AuthorizedPayment();
        _payments.AddReconciliationRecord(PendingRecord(payment));
        _providerFactory.Provider.Transactions.Add(new ProviderTransaction(
            "pi_1", "authorized", 12.00m, "USD", "succeeded", UtcNow.AddMinutes(-5)));

        var report = await _service.RunAsync(CancellationToken.None);

        Assert.Equal(1, report.DriftCount);
        Assert.Single(report.Drifts);
        Assert.True(report.HasDrift);
        var record = Assert.Single(_payments.ReconciliationRecords);
        Assert.Equal(ReconciliationStatus.Drift, record.Status);
        Assert.Contains("12.00 USD", record.Detail);
    }

    [Fact]
    public async Task Run_Marks_Missing_Provider_Transaction_As_Unmatched()
    {
        var payment = AuthorizedPayment();
        _payments.AddReconciliationRecord(PendingRecord(payment));

        var report = await _service.RunAsync(CancellationToken.None);

        Assert.Equal(1, report.UnmatchedCount);
        Assert.True(report.HasDrift);
        var record = Assert.Single(_payments.ReconciliationRecords);
        Assert.Equal(ReconciliationStatus.Unmatched, record.Status);
    }

    [Fact]
    public async Task Run_Counts_Provider_Only_Transactions()
    {
        var payment = AuthorizedPayment();
        _payments.AddReconciliationRecord(PendingRecord(payment));
        _providerFactory.Provider.Transactions.Add(new ProviderTransaction(
            "pi_1", "authorized", 39.90m, "USD", "succeeded", UtcNow.AddMinutes(-5)));
        _providerFactory.Provider.Transactions.Add(new ProviderTransaction(
            "pi_orphan", "authorized", 5.00m, "USD", "succeeded", UtcNow.AddMinutes(-3)));

        var report = await _service.RunAsync(CancellationToken.None);

        Assert.Equal(1, report.ProviderOnlyCount);
        Assert.Equal(1, report.MatchedCount);
    }

    [Fact]
    public async Task Run_Snapshots_Unreconciled_Payments_First()
    {
        _ = AuthorizedPayment();
        _providerFactory.Provider.Transactions.Add(new ProviderTransaction(
            "pi_1", "authorized", 39.90m, "USD", "succeeded", UtcNow.AddMinutes(-5)));

        var report = await _service.RunAsync(CancellationToken.None);

        Assert.Equal(1, report.MatchedCount);
        var record = Assert.Single(_payments.ReconciliationRecords);
        Assert.Equal(ReconciliationStatus.Matched, record.Status);
    }

    [Fact]
    public async Task Run_No_Pending_Records_Returns_Empty_Report()
    {
        var report = await _service.RunAsync(CancellationToken.None);

        Assert.Equal(0, report.MatchedCount);
        Assert.Equal(0, report.DriftCount);
        Assert.Empty(report.Drifts);
        Assert.False(report.HasDrift);
    }

    [Fact]
    public async Task Run_Writes_Reconciliation_Audit_Trail()
    {
        var payment = AuthorizedPayment();
        var record = PendingRecord(payment);
        _payments.AddReconciliationRecord(record);
        _providerFactory.Provider.Transactions.Add(new ProviderTransaction(
            "pi_1", "authorized", 12.00m, "USD", "succeeded", UtcNow.AddMinutes(-5)));

        await _service.RunAsync(CancellationToken.None);

        Assert.Equal(2, _audit.Operations.Count);
        Assert.Equal("finance.reconciliation.run", _audit.Operations[0].Action);
        Assert.Equal("finance.reconciliation.drift", _audit.Operations[1].Action);
        Assert.Equal(record.Id.ToString(), _audit.Operations[1].EntityId);
    }

    [Fact]
    public async Task Run_Unavailable_Provider_Flags_Drift()
    {
        var payment = AuthorizedPayment();
        _payments.AddReconciliationRecord(PendingRecord(payment));
        _providerFactory.MissingKey = "mock";

        var report = await _service.RunAsync(CancellationToken.None);

        Assert.Equal(1, report.DriftCount);
        var record = Assert.Single(_payments.ReconciliationRecords);
        Assert.Equal(ReconciliationStatus.Drift, record.Status);
    }
}
