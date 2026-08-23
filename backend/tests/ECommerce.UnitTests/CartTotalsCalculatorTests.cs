using ECommerce.Domain.Cart;
using ECommerce.UseCases.Cart.Responses;
using ECommerce.UseCases.Pricing;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UnitTests;

public sealed class CartTotalsCalculatorTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ICurrencyCatalog Currencies = new DefaultCurrencyCatalog();

    [Fact]
    public void Compute_Empty_Cart_Returns_Zero_Subtotal_With_Shipping()
    {
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);

        var totals = CartTotalsCalculator.Compute(cart, Currencies);

        Assert.Equal(0m, totals.Subtotal);
        Assert.Equal(0m, totals.ItemDiscount);
        Assert.Equal(9.90m, totals.Shipping);
        Assert.Equal(0m, totals.Tax);
        Assert.Equal(9.90m, totals.Total);
    }

    [Fact]
    public void Compute_Includes_Item_Discount_From_List_And_Unit_Price()
    {
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "SKU-1", "Widget", 20.00m, 15.00m, 2, null, UtcNow);

        var totals = CartTotalsCalculator.Compute(cart, Currencies);

        Assert.Equal(40.00m, totals.Subtotal);
        Assert.Equal(10.00m, totals.ItemDiscount);
        Assert.Equal(9.90m, totals.Shipping);
        Assert.Equal(1.50m, totals.Tax);
        Assert.Equal(41.40m, totals.Total);
    }

    [Fact]
    public void Compute_Converts_Flat_Shipping_To_Cart_Currency()
    {
        var cart = CartAggregate.Create("anon-1", "AED", UtcNow.AddDays(30), UtcNow);
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "SKU-1", "Widget", 36.725m, 36.725m, 1, null, UtcNow);

        var totals = CartTotalsCalculator.Compute(cart, Currencies);

        Assert.Equal(36.73m, totals.Subtotal);
        Assert.Equal(36.36m, totals.Shipping);
        Assert.Equal(1.84m, totals.Tax);
        Assert.Equal(74.92m, totals.Total);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("AED")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("EGP")]
    public void Compute_Produces_Totals_In_Every_Supported_Currency(string currency)
    {
        var cart = CartAggregate.Create("anon-1", currency, UtcNow.AddDays(30), UtcNow);
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "SKU-1", "Widget", 20.00m, 15.00m, 2, null, UtcNow);

        var totals = CartTotalsCalculator.Compute(cart, Currencies);

        Assert.True(Currencies.IsSupported(currency));
        Assert.True(totals.Subtotal > 0m);
        Assert.True(totals.Shipping > 0m);
        Assert.True(totals.Total > 0m);
        Assert.True(totals.ItemDiscount >= 0m);
        Assert.True(totals.Tax >= 0m);
        var expected = totals.Subtotal - totals.ItemDiscount + totals.Shipping + totals.Tax;
        Assert.InRange(totals.Total, expected - 0.02m, expected + 0.02m);
    }
}
