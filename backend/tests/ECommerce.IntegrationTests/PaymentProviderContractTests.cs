using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Payments;

namespace ECommerce.IntegrationTests;

[Collection("Integration")]
public sealed class PaymentProviderContractTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Mock_Creates_Intent_With_Client_Token_And_Authorizes()
    {
        var provider = new MockPaymentProvider();

        var intent = await provider.CreateIntentAsync(
            new PaymentIntentRequest(129.90m, "USD", "checkout-1", "card", null),
            CancellationToken.None);

        Assert.True(intent.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(intent.ClientToken));
        Assert.False(string.IsNullOrWhiteSpace(intent.ProviderToken));
        Assert.StartsWith("mock_ct_", intent.ClientToken);

        var authorization = await provider.AuthorizeAsync(
            new PaymentAuthorizationRequest(129.90m, "USD", intent.ProviderToken, "checkout-1"),
            CancellationToken.None);

        Assert.True(authorization.IsSuccess);
        Assert.StartsWith("mock_auth_", authorization.ProviderReference);
    }

    [Fact]
    public async Task Mock_Declines_Token_With_Decline_Prefix()
    {
        var provider = new MockPaymentProvider();

        var authorization = await provider.AuthorizeAsync(
            new PaymentAuthorizationRequest(129.90m, "USD", $"{MockPaymentProvider.MockDeclineTokenPrefix}tok", "checkout-1"),
            CancellationToken.None);

        Assert.False(authorization.IsSuccess);
        Assert.Equal("card_declined", authorization.DeclineCode);
    }

    [Fact]
    public async Task Mock_Provider_Token_Is_Opaque_And_Masked()
    {
        var provider = new MockPaymentProvider();

        var intent = await provider.CreateIntentAsync(
            new PaymentIntentRequest(10m, "USD", "checkout-2", "card", null),
            CancellationToken.None);

        var token = new PaymentToken("mock", intent.ProviderToken);
        var masked = token.ToString();

        Assert.DoesNotContain(intent.ProviderToken, masked, StringComparison.Ordinal);
        Assert.Contains("****", masked, StringComparison.Ordinal);
        Assert.EndsWith(intent.ProviderToken[^4..], masked, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Stripe_Creates_Intent_And_Authorizes_In_Test_Mode()
    {
        var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        Skip.If(string.IsNullOrWhiteSpace(secretKey), "STRIPE_SECRET_KEY is not set");

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
        var provider = new StripePaymentProvider(secretKey, factory);

        var intent = await provider.CreateIntentAsync(
            new PaymentIntentRequest(129.90m, "USD", $"contract-{Guid.NewGuid():N}", "card", null),
            CancellationToken.None);

        Assert.True(intent.IsSuccess, $"Stripe intent creation failed: {intent.ErrorCode}");
        Assert.False(string.IsNullOrWhiteSpace(intent.ClientToken));
        Assert.StartsWith("pi_", intent.ProviderToken);

        var authorization = await provider.AuthorizeAsync(
            new PaymentAuthorizationRequest(129.90m, "USD", intent.ProviderToken, $"contract-{Guid.NewGuid():N}"),
            CancellationToken.None);

        Assert.True(authorization.IsSuccess, "A freshly created unconfirmed intent is not yet authorized");
        Assert.Equal(intent.ProviderToken, authorization.ProviderReference);
    }
}
