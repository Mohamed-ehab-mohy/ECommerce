using ECommerce.Domain.Cart;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Cart.Commands;
using ECommerce.UseCases.Checkout.Commands;
using ECommerce.UseCases.Checkout.Handlers;
using ECommerce.UseCases.Checkout.Services;
using ECommerce.UseCases.Payments.Services;
using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UnitTests;

public sealed class InitiateCheckoutCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly FakeCartRepository _carts = new();

    private readonly FakeCheckoutRepository _checkouts = new();

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakePaymentProviderFactory _paymentFactory = new();

    private readonly FakeStockRepository _stock = new();

    private readonly FakeProductRepository _products = new();

    private readonly FakePromotionRepository _promotions = new();

    private readonly FakeCouponRepository _coupons = new();

    private static readonly AddressInput Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private InitiateCheckoutCommandHandler CreateHandler()
    {
        var paymentIntents = new PaymentIntentService(_paymentFactory, new FakePaymentProviderHealth(), TimeProvider.System);
        var totals = new CheckoutTotalsCalculator(new FakeShippingRateProvider(), new FakeTaxCalculator());
        var availability = new StockAvailabilityVerifier(_stock);

        return new InitiateCheckoutCommandHandler(
            _carts,
            _checkouts,
            _payments,
            _products,
            _promotions,
            _coupons,
            paymentIntents,
            totals,
            availability,
            _unitOfWork,
            TimeProvider.System,
            new InitiateCheckoutCommandValidator());
    }

    private CartAggregate CreateCart(string sku = "SKU-1", int quantity = 2)
    {
        var cart = CartAggregate.Create("anon:1", "USD", UtcNow.AddDays(30), UtcNow);
        cart.AddItem(Guid.NewGuid(), sku, "Widget", 20.00m, 15.00m, quantity, null, UtcNow);
        return cart;
    }

    private static StockItem CreateStockedItem(string sku, int onHand)
    {
        var item = StockItem.Create(sku, WarehouseId, UtcNow);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Receipt, onHand, "RECV", null, null, UtcNow), UtcNow);
        return item;
    }

    private static InitiateCheckoutCommand CreateCommand(Guid cartId, string shippingMethod = "standard") =>
        new(
            cartId,
            null,
            "ahmed@example.com",
            "USD",
            Address,
            null,
            shippingMethod,
            "mock",
            "card",
            "AE");

    [Fact]
    public async Task Initiate_Success_Creates_Checkout_And_Payment()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);
        _stock.Items.Add(CreateStockedItem("SKU-1", 10));

        var result = await CreateHandler().Handle(CreateCommand(cart.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(cart.Id, result.Value.CartId);
        Assert.Equal(CheckoutStatus.Created, result.Value.Status);
        Assert.Equal("tok_mock_1", result.Value.Payment.ClientToken);
        Assert.Equal("mock", result.Value.Payment.ProviderKey);
        Assert.NotEqual(Guid.Empty, result.Value.Payment.PaymentId);
        Assert.Equal(40.00m, result.Value.Totals.Subtotal);
        Assert.Equal(39.90m, result.Value.Totals.GrandTotal);

        var checkout = Assert.Single(_checkouts.Checkouts);
        Assert.Equal(result.Value.CheckoutId, checkout.Id);
        Assert.Equal(cart.Id, checkout.CartId);

        var payment = Assert.Single(_payments.Payments);
        Assert.Equal(checkout.PaymentId, payment.Id);
        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Initiate_Unknown_Cart_Returns_NotFound()
    {
        var result = await CreateHandler().Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.CartNotFound, result.Error);
        Assert.Empty(_checkouts.Checkouts);
        Assert.Empty(_payments.Payments);
    }

    [Fact]
    public async Task Initiate_Empty_Cart_Returns_Conflict()
    {
        var cart = CartAggregate.Create("anon:1", "USD", UtcNow.AddDays(30), UtcNow);
        _carts.Carts.Add(cart);

        var result = await CreateHandler().Handle(CreateCommand(cart.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.CartEmpty, result.Error);
    }

    [Fact]
    public async Task Initiate_Insufficient_Stock_Returns_409_With_Lines()
    {
        var cart = CreateCart(quantity: 5);
        _carts.Carts.Add(cart);
        _stock.Items.Add(CreateStockedItem("SKU-1", 3));

        var result = await CreateHandler().Handle(CreateCommand(cart.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ERR_STK_001", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.NotNull(result.Error.Metadata);
        var lines = Assert.IsType<List<Dictionary<string, object?>>>(result.Error.Metadata["lines"]);
        var line = Assert.Single(lines);
        Assert.Equal("SKU-1", line["sku"]);
        Assert.Equal(5, line["requested"]);
        Assert.Equal(3, line["available"]);
    }

    [Fact]
    public async Task Initiate_Unsupported_Shipping_Method_Returns_Error()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);
        _stock.Items.Add(CreateStockedItem("SKU-1", 10));

        var result = await CreateHandler().Handle(CreateCommand(cart.Id, "courier-express"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.ShippingMethodUnsupported, result.Error);
    }

    [Fact]
    public async Task Initiate_Provider_Unavailable_Returns_BadGateway()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);
        _stock.Items.Add(CreateStockedItem("SKU-1", 10));
        _paymentFactory.MissingKey = "mock";

        var result = await CreateHandler().Handle(CreateCommand(cart.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.ProviderUnavailable, result.Error);
        Assert.Empty(_checkouts.Checkouts);
    }

    [Fact]
    public async Task Initiate_Declined_Provider_Returns_Declined()
    {
        var cart = CreateCart();
        _carts.Carts.Add(cart);
        _stock.Items.Add(CreateStockedItem("SKU-1", 10));
        _paymentFactory.Provider.IntentResult = new PaymentIntentResult(false, string.Empty, string.Empty, null, "decline");

        var result = await CreateHandler().Handle(CreateCommand(cart.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.PaymentDeclined, result.Error);
    }
}
