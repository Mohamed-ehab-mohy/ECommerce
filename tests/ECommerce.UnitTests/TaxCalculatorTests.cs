using ECommerce.Infrastructure.Orders;

namespace ECommerce.UnitTests;

public sealed class TaxCalculatorTests
{
    [Theory]
    [InlineData(100.00, "EG", 0.14, 14.00)]
    [InlineData(100.00, "SA", 0.15, 15.00)]
    [InlineData(100.00, "AE", 0.05, 5.00)]
    [InlineData(100.00, "US", 0.0825, 8.25)]
    [InlineData(100.00, "UK", 0.20, 20.00)]
    [InlineData(250.00, "EG", 0.14, 35.00)]
    [InlineData(0.00, "EG", 0.14, 0.00)]
    public async Task Compute_Applies_Rate_For_Known_Country(decimal taxable, string country, decimal expectedRate, decimal expectedAmount)
    {
        var calculator = new TaxCalculator(new StaticTaxRateProvider());

        var result = await calculator.ComputeAsync(taxable, country, "USD", CancellationToken.None);

        Assert.Equal(expectedRate, result.Rate);
        Assert.Equal(expectedAmount, result.Amount);
    }

    [Fact]
    public async Task Compute_Unknown_Country_Uses_Default_Rate()
    {
        var calculator = new TaxCalculator(new StaticTaxRateProvider());

        var result = await calculator.ComputeAsync(100.00m, "ZZ", "USD", CancellationToken.None);

        Assert.Equal(StaticTaxRateProvider.DefaultRate, result.Rate);
        Assert.Equal(Math.Round(100.00m * StaticTaxRateProvider.DefaultRate, 2), result.Amount);
    }

    [Fact]
    public async Task Compute_Configured_Rates_Override_BuiltIn()
    {
        var configured = new Dictionary<string, decimal>
        {
            ["EG"] = 0.05m
        };
        var calculator = new TaxCalculator(new StaticTaxRateProvider(configuredRates: configured));

        var result = await calculator.ComputeAsync(100.00m, "EG", "USD", CancellationToken.None);

        Assert.Equal(0.05m, result.Rate);
        Assert.Equal(5.00m, result.Amount);
    }

    [Fact]
    public async Task Compute_Negative_Taxable_Returns_Zero()
    {
        var calculator = new TaxCalculator(new StaticTaxRateProvider());

        var result = await calculator.ComputeAsync(-10.00m, "EG", "USD", CancellationToken.None);

        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task Compute_Rounds_Half_Away_From_Zero()
    {
        var calculator = new TaxCalculator(new StaticTaxRateProvider(configuredRates: new Dictionary<string, decimal>
        {
            ["ZZ"] = 0.5m
        }));

        var result = await calculator.ComputeAsync(0.25m, "ZZ", "USD", CancellationToken.None);

        Assert.Equal(0.13m, result.Amount);
    }

    [Fact]
    public async Task Compute_Empty_Country_Throws()
    {
        var calculator = new TaxCalculator(new StaticTaxRateProvider());

        await Assert.ThrowsAsync<ArgumentException>(() => calculator.ComputeAsync(100m, " ", "USD", CancellationToken.None));
    }
}
