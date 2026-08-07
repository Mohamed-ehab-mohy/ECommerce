using ECommerce.Domain.Orders;
using ECommerce.UseCases.Checkout.Services;

namespace ECommerce.UnitTests;

public sealed class CheckoutTotalsCalculatorTests
{
    private static readonly PriceSnapshotItem Widget = new(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null);

    [Fact]
    public async Task Compute_Calculates_Subtotal_Discount_Shipping_And_GrandTotal()
    {
        var calculator = new CheckoutTotalsCalculator(
            new FakeShippingRateProvider(),
            new FakeTaxCalculator(3.00m));

        var result = await calculator.ComputeAsync([Widget], "standard", "AE", "USD", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(40.00m, result.Value.Subtotal);
        Assert.Equal(10.00m, result.Value.ItemDiscount);
        Assert.Equal(0m, result.Value.CartDiscount);
        Assert.Equal(9.90m, result.Value.ShippingTotal);
        Assert.Equal(3.00m, result.Value.TaxTotal);
        Assert.Equal(42.90m, result.Value.GrandTotal);
    }

    [Fact]
    public async Task Compute_Unsupported_Shipping_Method_Returns_Error()
    {
        var calculator = new CheckoutTotalsCalculator(
            new FakeShippingRateProvider(),
            new FakeTaxCalculator());

        var result = await calculator.ComputeAsync([Widget], "courier-express", "AE", "USD", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.ShippingMethodUnsupported, result.Error);
    }

    [Fact]
    public async Task Compute_Empty_Lines_Returns_Zero_Base_Totals()
    {
        var calculator = new CheckoutTotalsCalculator(
            new FakeShippingRateProvider(),
            new FakeTaxCalculator());

        var result = await calculator.ComputeAsync([], "standard", "AE", "USD", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Subtotal);
        Assert.Equal(0m, result.Value.ItemDiscount);
        Assert.Equal(9.90m, result.Value.GrandTotal);
    }
}
