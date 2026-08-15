using System.Collections.Concurrent;
using ECommerce.Domain.Payments;

namespace ECommerce.Infrastructure.Payments;

public sealed class MockPaymentProvider : IPaymentProvider
{
    public const string MockDeclineTokenPrefix = "decline_";

    public const string MockRefundFailPrefix = "mock_decline_";

    private readonly ConcurrentDictionary<string, ProviderTransaction> _transactions = new();

    public string Key => "mock";

    public Task<PaymentIntentResult> CreateIntentAsync(
        PaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        return Task.FromResult(new PaymentIntentResult(
            true,
            $"mock_ct_{id}",
            $"mock_tok_{id}",
            $"mock_intent_{id}",
            null));
    }

    public Task<PaymentAuthorizationResult> AuthorizeAsync(
        PaymentAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var declined = request.ProviderToken.StartsWith(MockDeclineTokenPrefix, StringComparison.Ordinal);
        var reference = declined ? $"mock_decline_{Guid.NewGuid():N}" : $"mock_auth_{Guid.NewGuid():N}";

        Record(reference, "authorized", request.Amount, request.Currency, declined ? "declined" : "succeeded");

        return Task.FromResult(
            declined
                ? new PaymentAuthorizationResult(false, reference, "card_declined")
                : new PaymentAuthorizationResult(true, reference, null));
    }

    public Task<PaymentRefundResult> RefundAsync(
        PaymentRefundRequest request,
        CancellationToken cancellationToken)
    {
        var fails = request.ProviderReference.StartsWith(MockRefundFailPrefix, StringComparison.Ordinal);
        var reference = fails ? $"mock_refund_fail_{Guid.NewGuid():N}" : $"mock_refund_{Guid.NewGuid():N}";

        Record(reference, "refunded", request.Amount, request.Currency, fails ? "failed" : "succeeded");

        return Task.FromResult(
            fails
                ? new PaymentRefundResult(false, reference, "refund_failed")
                : new PaymentRefundResult(true, reference, null));
    }

    public Task<IReadOnlyList<ProviderTransaction>> ListTransactionsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProviderTransaction>>(_transactions.Values
            .Where(transaction => transaction.OccurredAt >= fromUtc && transaction.OccurredAt <= toUtc)
            .OrderBy(transaction => transaction.OccurredAt)
            .ToList());

    private void Record(string reference, string eventType, decimal amount, string currency, string status) =>
        _transactions[reference] = new ProviderTransaction(
            reference,
            eventType,
            amount,
            currency,
            status,
            DateTime.UtcNow);
}
