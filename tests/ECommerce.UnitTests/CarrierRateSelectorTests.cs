using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.UnitTests;

public sealed class CarrierRateSelectorTests
{
    private static readonly DateTime BaseUtc = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    private static CarrierShipmentRequest Request =>
        new("AE", "SA", "11461", 1200, "SAR", []);

    private static CarrierQuoteResult Quote(string carrierKey, decimal amount) =>
        new(carrierKey, amount, "SAR", BaseUtc.AddDays(2));

    [Fact]
    public async Task Select_Picks_Cheapest_Carrier()
    {
        var aramex = new FakeCarrierAdapter("aramex", Quote("aramex", 30m));
        var dhl = new FakeCarrierAdapter("dhl", Quote("dhl", 22m));
        var selector = new CarrierRateSelector([aramex, dhl], new FakeShippingRateCache());

        var result = await selector.SelectAsync(Request, CancellationToken.None);

        Assert.NotNull(result.Cheapest);
        Assert.Equal("dhl", result.Cheapest.CarrierKey);
        Assert.Equal(22m, result.Cheapest.Amount);
        Assert.Empty(result.UnavailableCarriers);
        Assert.False(result.IsFallback);
    }

    [Fact]
    public async Task Select_Serves_Second_Quote_From_Cache()
    {
        var aramex = new FakeCarrierAdapter("aramex", Quote("aramex", 30m));
        var dhl = new FakeCarrierAdapter("dhl", Quote("dhl", 22m));
        var cache = new FakeShippingRateCache();
        var selector = new CarrierRateSelector([aramex, dhl], cache);

        await selector.SelectAsync(Request, CancellationToken.None);
        var second = await selector.SelectAsync(Request, CancellationToken.None);

        Assert.True(second.FromCache);
        Assert.Equal(1, aramex.QuoteCallCount);
        Assert.Equal(1, dhl.QuoteCallCount);
        Assert.Equal(2, cache.Quotes.Count);
    }

    [Fact]
    public async Task Select_Marks_Unavailable_Carriers_As_Fallback()
    {
        var broken = new FakeCarrierAdapter("aramex") { ThrowOnQuote = true };
        var healthy = new FakeCarrierAdapter("dhl", Quote("dhl", 22m));
        var selector = new CarrierRateSelector([broken, healthy], new FakeShippingRateCache());

        var result = await selector.SelectAsync(Request, CancellationToken.None);

        Assert.NotNull(result.Cheapest);
        Assert.Equal("dhl", result.Cheapest.CarrierKey);
        Assert.True(result.IsFallback);
        Assert.Contains("aramex", result.UnavailableCarriers);
    }

    [Fact]
    public async Task Select_Returns_Null_When_All_Carriers_Fail()
    {
        var aramex = new FakeCarrierAdapter("aramex") { ThrowOnQuote = true };
        var dhl = new FakeCarrierAdapter("dhl") { ThrowOnQuote = true };
        var selector = new CarrierRateSelector([aramex, dhl], new FakeShippingRateCache());

        var result = await selector.SelectAsync(Request, CancellationToken.None);

        Assert.Null(result.Cheapest);
        Assert.Equal(2, result.UnavailableCarriers.Count);
    }

    [Fact]
    public async Task Select_Retries_Unavailable_Carrier_On_Next_Quote()
    {
        var broken = new FakeCarrierAdapter("aramex") { ThrowOnQuote = true };
        var healthy = new FakeCarrierAdapter("dhl", Quote("dhl", 22m));
        var selector = new CarrierRateSelector([broken, healthy], new FakeShippingRateCache());

        await selector.SelectAsync(Request, CancellationToken.None);
        await selector.SelectAsync(Request, CancellationToken.None);

        Assert.Equal(2, broken.QuoteCallCount);
        Assert.Equal(1, healthy.QuoteCallCount);
    }
}
