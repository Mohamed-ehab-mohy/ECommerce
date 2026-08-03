using ECommerce.UseCases.Pricing;

namespace ECommerce.UnitTests;

public sealed class CurrencyCatalogTests
{
    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private readonly ILocaleCatalog _locales = new DefaultLocaleCatalog();

    [Fact]
    public void CurrencyCatalog_Exposes_Five_Supported_Currencies()
    {
        Assert.Collection(
            _currencies.SupportedCurrencies,
            item => Assert.Equal("USD", item),
            item => Assert.Equal("EUR", item),
            item => Assert.Equal("GBP", item),
            item => Assert.Equal("AED", item),
            item => Assert.Equal("EGP", item));
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("AED")]
    [InlineData("EGP")]
    [InlineData("usd")]
    public void CurrencyCatalog_Supports_Configured_Currencies(string currency)
    {
        Assert.True(_currencies.IsSupported(currency));
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("")]
    [InlineData("  ")]
    public void CurrencyCatalog_Rejects_Unknown_Currencies(string currency)
    {
        Assert.False(_currencies.IsSupported(currency));
    }

    [Fact]
    public void GetRate_Between_Same_Currency_Is_One()
    {
        Assert.Equal(1m, _currencies.GetRate("USD", "USD"));
    }

    [Fact]
    public void GetRate_Converts_From_Base_To_Target()
    {
        Assert.Equal(3.6725m, _currencies.GetRate("USD", "AED"));
        Assert.Equal(48.5m, _currencies.GetRate("USD", "EGP"));
    }

    [Fact]
    public void GetRate_Converts_Between_Non_Base_Currencies()
    {
        var rate = _currencies.GetRate("AED", "USD");

        Assert.InRange(rate, 0.27m, 0.28m);
    }

    [Fact]
    public void GetRate_Is_Case_Insensitive()
    {
        Assert.Equal(3.6725m, _currencies.GetRate("usd", "aed"));
    }

    [Fact]
    public void GetRate_Throws_For_Unknown_Source_Or_Target()
    {
        Assert.Throws<ArgumentException>(() => _currencies.GetRate("JPY", "USD"));
        Assert.Throws<ArgumentException>(() => _currencies.GetRate("USD", "JPY"));
    }

    [Fact]
    public void LocaleCatalog_Exposes_Default_Locale()
    {
        Assert.Equal("en", _locales.DefaultLocale);
    }

    [Fact]
    public void LocaleCatalog_Exposes_Ten_Supported_Locales()
    {
        Assert.Equal(10, _locales.SupportedLocales.Count);
        Assert.Collection(
            _locales.SupportedLocales,
            item => Assert.Equal("en", item),
            item => Assert.Equal("ar", item),
            item => Assert.Equal("fr", item),
            item => Assert.Equal("de", item),
            item => Assert.Equal("es", item),
            item => Assert.Equal("it", item),
            item => Assert.Equal("pt", item),
            item => Assert.Equal("tr", item),
            item => Assert.Equal("ru", item),
            item => Assert.Equal("zh", item));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ar")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("pt")]
    [InlineData("tr")]
    [InlineData("ru")]
    [InlineData("zh")]
    [InlineData("AR")]
    public void LocaleCatalog_Supports_Configured_Locales(string locale)
    {
        Assert.True(_locales.IsSupported(locale));
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("ja")]
    [InlineData("")]
    public void LocaleCatalog_Rejects_Unknown_Locales(string locale)
    {
        Assert.False(_locales.IsSupported(locale));
    }
}
