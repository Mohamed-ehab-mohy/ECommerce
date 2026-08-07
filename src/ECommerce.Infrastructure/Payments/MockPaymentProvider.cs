using ECommerce.Domain.Payments;

namespace ECommerce.Infrastructure.Payments;

public sealed class MockPaymentProvider : IPaymentProvider
{
    public const string MockDeclineTokenPrefix = "decline_";

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
        CancellationToken cancellationToken) =>
        Task.FromResult(
            request.ProviderToken.StartsWith(MockDeclineTokenPrefix, StringComparison.Ordinal)
                ? new PaymentAuthorizationResult(false, $"mock_decline_{Guid.NewGuid():N}", "card_declined")
                : new PaymentAuthorizationResult(true, $"mock_auth_{Guid.NewGuid():N}", null));
}
