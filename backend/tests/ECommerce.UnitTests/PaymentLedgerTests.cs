using ECommerce.Domain.Payments;

namespace ECommerce.UnitTests;

public sealed class PaymentLedgerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

    private static Payment CreatePayment() =>
        Payment.Create(null, "mock", "tok_1", "ct_1", "pi_1", "USD", 39.90m, null, UtcNow);

    [Fact]
    public void Create_Records_Intent_Created_Entry()
    {
        var payment = CreatePayment();

        var entry = Assert.Single(payment.Ledger);
        Assert.Equal("intent_created", entry.EventType);
        Assert.Equal("created", entry.Status);
        Assert.Equal(39.90m, entry.Amount);
        Assert.Equal("pi_1", entry.ProviderReference);
        Assert.Equal(1, entry.Sequence);
    }

    [Fact]
    public void Authorize_Appends_Authorized_Entry()
    {
        var payment = CreatePayment();
        payment.MarkAuthorized("pi_1", UtcNow);

        Assert.Equal(2, payment.Ledger.Count);
        Assert.Equal("authorized", payment.Ledger.Last().EventType);
        Assert.Equal("authorized", payment.Ledger.Last().Status);
        Assert.Equal("pi_1", payment.Ledger.Last().ProviderReference);
        Assert.Equal(2, payment.Ledger.Last().Sequence);
    }

    [Fact]
    public void Failed_Appends_Entry_With_Decline_Detail()
    {
        var payment = CreatePayment();
        payment.MarkFailed(UtcNow, "card_declined");

        var entry = Assert.Single(payment.Ledger, e => e.EventType == "failed");
        Assert.Equal("failed", entry.Status);
        Assert.Equal("card_declined", entry.Detail);
    }

    [Fact]
    public void Capture_And_Void_Append_Entries()
    {
        var payment = CreatePayment();
        payment.MarkAuthorized("pi_1", UtcNow);
        payment.Capture(39.90m, UtcNow);

        Assert.Equal("captured", payment.Ledger.Last().EventType);
        Assert.Equal("captured", payment.Ledger.Last().Status);

        var voided = CreatePayment();
        voided.MarkAuthorized("pi_2", UtcNow);
        voided.Void(UtcNow);

        Assert.Equal("voided", voided.Ledger.Last().EventType);
        Assert.Equal("voided", voided.Ledger.Last().Status);
    }

    [Fact]
    public void Ledger_Is_Append_Only_With_Monotonic_Sequence()
    {
        var payment = CreatePayment();
        payment.MarkFailed(UtcNow, "card_declined");
        payment.MarkAuthorized("pi_1", UtcNow);

        Assert.Equal(3, payment.Ledger.Count);
        Assert.Equal(new[] { 1, 2, 3 }, payment.Ledger.Select(entry => entry.Sequence));
        Assert.Equal(
            new[] { "intent_created", "failed", "authorized" },
            payment.Ledger.Select(entry => entry.EventType));
    }
}


