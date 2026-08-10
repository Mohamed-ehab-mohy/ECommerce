using ECommerce.Domain.Events;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Handlers;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UnitTests;

public sealed class PlaceOrderCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeCheckoutRepository _checkouts = new();

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeIdempotencyKeyRepository _idempotencyKeys = new();

    private readonly FakeStockAllocator _allocator = new();

    private readonly FakeOrderNumberGenerator _orderNumberGenerator = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m));

    private PlaceOrderCommandHandler CreateHandler() =>
        new(
            _checkouts,
            _payments,
            _orders,
            _idempotencyKeys,
            _allocator,
            _orderNumberGenerator,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new PlaceOrderCommandValidator());

    private (CheckoutAggregate Checkout, Payment Payment) CreateAuthorizedCheckout(bool authorizePayment = true)
    {
        var payment = Payment.Create(
            null, "mock", "tok_mock_1", "tok_client_1", null, "USD", 39.90m, null, UtcNow);

        if (authorizePayment)
        {
            payment.MarkAuthorized("pi_mock_1_auth", UtcNow);
        }

        var checkout = CheckoutAggregate.Create(
            Guid.NewGuid(),
            null,
            "ahmed@example.com",
            "USD",
            Snapshot,
            Address,
            Address,
            "standard",
            payment.Id,
            UtcNow.AddMinutes(30),
            UtcNow);
        checkout.MarkPaymentAuthorized(UtcNow);

        _payments.Add(payment);
        _checkouts.Add(checkout);
        return (checkout, payment);
    }

    private static PlaceOrderCommand CreateCommand(Guid checkoutId, string key = "order-key-1") =>
        new(checkoutId, key);

    [Fact]
    public async Task Place_Success_Creates_Order_Allocates_Stock_And_Places_Checkout()
    {
        var (checkout, payment) = CreateAuthorizedCheckout();

        var result = await CreateHandler().Handle(CreateCommand(checkout.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(checkout.CartId, result.Value.CartId);
        Assert.Equal(39.90m, result.Value.GrandTotal);
        Assert.Equal(OrderStatus.Placed.ToString(), result.Value.Status);
        Assert.StartsWith("E-20260807-", result.Value.OrderNumber);
        Assert.NotNull(result.Value.PlacedAt);
        var line = Assert.Single(result.Value.Lines);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal(2, line.Quantity);

        var order = Assert.Single(_orders.Orders);
        Assert.Equal(order.Id, result.Value.OrderId);
        Assert.Equal(checkout.Id, order.CheckoutId);
        Assert.Equal(payment.Id, order.PaymentId);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Single(order.Items);
        Assert.Single(order.DomainEvents.OfType<OrderPlaced>());

        Assert.Equal(CheckoutStatus.Placed, checkout.Status);
        Assert.NotNull(checkout.PlacedAt);
        Assert.Equal(order.Id, payment.OrderId);
        Assert.Equal(PaymentStatus.Authorized, payment.Status);

        Assert.Equal(1, _allocator.AllocateCount);
        Assert.Equal("ORDER", _allocator.LastReason);
        Assert.Equal(order.Id.ToString("N"), _allocator.LastReference);
        var allocationLine = Assert.Single(_allocator.LastItems);
        Assert.Equal("SKU-1", allocationLine.Sku);
        Assert.Equal(2, allocationLine.Quantity);

        var key = Assert.Single(_idempotencyKeys.Keys);
        Assert.Equal("order-key-1", key.Key);
        Assert.Equal(checkout.Id, key.CheckoutId);
        Assert.Equal(order.Id, key.OrderId);

        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.NotNull(_unitOfWork.LastTransaction);
        Assert.Equal(1, _unitOfWork.LastTransaction.CommitCount);
    }

    [Fact]
    public async Task Place_With_Existing_Key_Replays_Existing_Order()
    {
        var (checkout, _) = CreateAuthorizedCheckout();

        var handler = CreateHandler();
        var first = await handler.Handle(CreateCommand(checkout.Id), CancellationToken.None);
        var second = await handler.Handle(CreateCommand(checkout.Id), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.OrderId, second.Value.OrderId);
        Assert.Equal(1, _allocator.AllocateCount);
        Assert.Single(_idempotencyKeys.Keys);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Place_Key_Reused_For_Different_Checkout_Returns_Conflict()
    {
        var (checkoutA, _) = CreateAuthorizedCheckout();

        var handler = CreateHandler();
        var first = await handler.Handle(CreateCommand(checkoutA.Id), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var paymentB = Payment.Create(null, "mock", "tok_2", "tok_client_2", null, "USD", 39.90m, null, UtcNow);
        paymentB.MarkAuthorized("pi_mock_2_auth", UtcNow);
        var checkoutB = CheckoutAggregate.Create(
            Guid.NewGuid(), null, "sara@example.com", "USD", Snapshot, Address, Address, "standard",
            paymentB.Id, UtcNow.AddMinutes(30), UtcNow);
        checkoutB.MarkPaymentAuthorized(UtcNow);
        _payments.Add(paymentB);
        _checkouts.Add(checkoutB);

        var second = await handler.Handle(CreateCommand(checkoutB.Id), CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal(OrderErrors.IdempotencyKeyReuse, second.Error);
        Assert.Equal(ErrorType.Conflict, second.Error.Type);
    }

    [Fact]
    public async Task Place_Expired_Checkout_Returns_Conflict()
    {
        var payment = Payment.Create(null, "mock", "tok_mock_1", "tok_client_1", null, "USD", 39.90m, null, UtcNow);
        payment.MarkAuthorized("pi_mock_1_auth", UtcNow);
        var checkout = CheckoutAggregate.Create(
            Guid.NewGuid(), null, "ahmed@example.com", "USD", Snapshot, Address, Address, "standard",
            payment.Id, UtcNow.AddMinutes(-1), UtcNow);
        checkout.MarkPaymentAuthorized(UtcNow);
        _payments.Add(payment);
        _checkouts.Add(checkout);

        var result = await CreateHandler().Handle(CreateCommand(checkout.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.CheckoutExpired, result.Error);
        Assert.Empty(_orders.Orders);
    }

    [Fact]
    public async Task Place_Unauthorized_Payment_Returns_Conflict()
    {
        var (checkout, _) = CreateAuthorizedCheckout(authorizePayment: false);

        var result = await CreateHandler().Handle(CreateCommand(checkout.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.PaymentNotAuthorized, result.Error);
        Assert.Empty(_orders.Orders);
    }

    [Fact]
    public async Task Place_Insufficient_Stock_Returns_409_With_Lines()
    {
        var (checkout, _) = CreateAuthorizedCheckout();
        var allocator = new FakeStockAllocator(shortfalls: [new StockShortfall("SKU-1", 2, 1)]);

        var handler = new PlaceOrderCommandHandler(
            _checkouts,
            _payments,
            _orders,
            _idempotencyKeys,
            allocator,
            _orderNumberGenerator,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new PlaceOrderCommandValidator());

        var result = await handler.Handle(CreateCommand(checkout.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ERR_STK_001", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.NotNull(result.Error.Metadata);
        var lines = Assert.IsType<List<Dictionary<string, object?>>>(result.Error.Metadata["lines"]);
        var line = Assert.Single(lines);
        Assert.Equal("SKU-1", line["sku"]);
        Assert.Equal(2, line["requested"]);
        Assert.Equal(1, line["available"]);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.NotNull(_unitOfWork.LastTransaction);
        Assert.Equal(0, _unitOfWork.LastTransaction.CommitCount);
    }

    [Fact]
    public async Task Place_Unknown_Checkout_Returns_NotFound()
    {
        var result = await CreateHandler().Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.CheckoutNotFound, result.Error);
    }

    [Fact]
    public async Task Place_Empty_Idempotency_Key_Fails_Validation()
    {
        var (checkout, _) = CreateAuthorizedCheckout();

        var result = await CreateHandler().Handle(CreateCommand(checkout.Id, string.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_orders.Orders);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
