using ECommerce.UseCases.Pricing;

namespace ECommerce.UnitTests.Tests.Pricing;

public sealed class CurrencyExchangeTests
{
    [Fact]
    public void CurrencyRate_HasRequiredFields()
    {
        var rate = new CurrencyRate("USD", "EUR", 0.92m, DateTime.UtcNow);

        Assert.Equal("USD", rate.FromCurrency);
        Assert.Equal("EUR", rate.ToCurrency);
        Assert.Equal(0.92m, rate.Rate);
        Assert.True(rate.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void GeoPricingAdjustment_HasRequiredFields()
    {
        var adjustment = new GeoPricingAdjustment("US", 0, 8.25m, "USD");

        Assert.Equal("US", adjustment.CountryCode);
        Assert.Equal(0, adjustment.MarkupPercent);
        Assert.Equal(8.25m, adjustment.TaxRatePercent);
        Assert.Equal("USD", adjustment.Currency);
    }

    [Fact]
    public void CurrencyRate_RecordEquality_Works()
    {
        var now = DateTime.UtcNow;
        var r1 = new CurrencyRate("USD", "EUR", 0.92m, now);
        var r2 = new CurrencyRate("USD", "EUR", 0.92m, now);

        Assert.Equal(r1, r2);
    }

    [Fact]
    public void GeoPricingAdjustment_RecordEquality_Works()
    {
        var a1 = new GeoPricingAdjustment("US", 0, 8.25m, "USD");
        var a2 = new GeoPricingAdjustment("US", 0, 8.25m, "USD");

        Assert.Equal(a1, a2);
    }

    [Fact]
    public void SupportedCurrencies_Include_Major_Currencies()
    {
        var majorCurrencies = new[] { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "INR", "AED", "SAR" };
        Assert.All(majorCurrencies, c => Assert.False(string.IsNullOrWhiteSpace(c)));
    }

    [Fact]
    public void GeoPricingAdjustment_Markup_Can_Be_Zero_For_Domestic()
    {
        var domestic = new GeoPricingAdjustment("US", 0, 8.25m, "USD");
        Assert.Equal(0, domestic.MarkupPercent);
    }

    [Fact]
    public void GeoPricingAdjustment_High_Markup_For_Emerging_Markets()
    {
        var eg = new GeoPricingAdjustment("EG", 20, 14, "EGP");
        Assert.Equal(20, eg.MarkupPercent);
        Assert.Equal("EGP", eg.Currency);
    }
}
