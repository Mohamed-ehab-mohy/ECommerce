using ECommerce.Domain.Cart;
using ECommerce.UseCases.Cart.Handlers;
using ECommerce.UseCases.Cart.Queries;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging.Abstractions;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UnitTests;

public sealed class CartQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeCartRepository _carts = new();

    private readonly ICurrencyCatalog _currencies = new DefaultCurrencyCatalog();

    private GetCartQueryHandler GetHandler => new(
        _carts,
        _currencies,
        TimeProvider.System,
        new GetCartQueryValidator(_currencies),
        NullLogger<GetCartQueryHandler>.Instance);

    [Fact]
    public async Task GetCart_With_No_Existing_Cart_Returns_Empty_Unpersisted_Cart()
    {
        var result = await GetHandler.Handle(new GetCartQuery("anon-1", "USD"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Empty(_carts.Carts);
    }

    [Fact]
    public async Task GetCart_Returns_Existing_Cart_With_Items()
    {
        var cart = CartAggregate.Create("anon-1", "USD", UtcNow.AddDays(30), UtcNow);
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "SKU-1", "Widget", 20.00m, 15.00m, 2, null, UtcNow);
        _carts.Carts.Add(cart);

        var result = await GetHandler.Handle(new GetCartQuery("anon-1", "USD"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cart.Id, result.Value.Id);
        Assert.Single(result.Value.Items);
        Assert.Equal(40.00m, result.Value.Totals.Subtotal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("XYZ")]
    public async Task GetCart_With_Unsupported_Currency_Returns_Validation_Error(string currency)
    {
        var result = await GetHandler.Handle(new GetCartQuery("anon-1", currency), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Failed", result.Error.Code);
    }
}
