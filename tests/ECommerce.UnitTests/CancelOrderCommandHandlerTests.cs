using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Handlers;

namespace ECommerce.UnitTests;

public sealed class CancelOrderCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeOrderRepository _orders = new();

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeStockAllocator _allocator = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m));

    private CancelOrderCommandHandler CreateHandler() =>
        new(
            _orders,
            _payments,
            _allocator,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new CancelOrderCommandValidator());

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private Order CreatePlacedOrder(Guid customerId, Guid? paymentId = null) =>
        Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            "ahmed@example.com",
            "USD",
            "E-20260807-000001",
            Snapshot,
            Address,
            Address,
            "standard",
            paymentId ?? Guid.NewGuid(),
            UtcNow);

    private static CancelOrderCommand CreateCommand(
        string orderNumber,
        Guid? requesterCustomerId,
        bool supportAccess = false,
        string? reason = "customer-request") =>
        new(orderNumber, reason, requesterCustomerId, supportAccess);

    [Fact]
    public async Task Handle_Cancels_Order_And_Releases_Stock()
    {
        var customerId = Guid.NewGuid();
        var order = CreatePlacedOrder(customerId);
        _orders.Add(order);
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, customerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value.Status);
        Assert.Equal(1, _allocator.ReleaseCount);
        Assert.Equal("ORDER-CANCELLED", _allocator.LastReason);
        Assert.Single(_allocator.LastItems);
        Assert.Equal("SKU-1", _allocator.LastItems[0].Sku);
        Assert.Equal(2, _allocator.LastItems[0].Quantity);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Captured_Payment_Requests_Refund()
    {
        var customerId = Guid.NewGuid();
        var order = CreatePlacedOrder(customerId);
        _orders.Add(order);

        var payment = Payment.Create(customerId, "mock", "tok_mock_1", "tok_client_1", null, "USD", 39.90m, null, UtcNow);
        payment.MarkAuthorized("pi_mock_1_auth", UtcNow);
        payment.Capture(39.90m, UtcNow);
        payment.AttachOrder(order.Id, UtcNow);
        _payments.Add(payment);

        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, customerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Refunding, payment.Status);
        Assert.Single(payment.Attempts);
    }

    [Fact]
    public async Task Handle_Authorized_Payment_Is_Not_Refunded()
    {
        var customerId = Guid.NewGuid();
        var order = CreatePlacedOrder(customerId);
        _orders.Add(order);

        var payment = Payment.Create(customerId, "mock", "tok_mock_1", "tok_client_1", null, "USD", 39.90m, null, UtcNow);
        payment.MarkAuthorized("pi_mock_1_auth", UtcNow);
        payment.AttachOrder(order.Id, UtcNow);
        _payments.Add(payment);

        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, customerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
    }

    [Fact]
    public async Task Handle_Other_Customer_Is_Rejected()
    {
        var order = CreatePlacedOrder(Guid.NewGuid());
        _orders.Add(order);
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotYourOrder, result.Error);
    }

    [Fact]
    public async Task Handle_Support_Access_Can_Cancel_Any_Order()
    {
        var order = CreatePlacedOrder(Guid.NewGuid());
        _orders.Add(order);
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, Guid.NewGuid(), supportAccess: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value.Status);
    }

    [Fact]
    public async Task Handle_Unknown_Order_Number_Returns_NotFound()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand("E-20260807-999999", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_Already_Cancelled_Order_Is_Rejected()
    {
        var customerId = Guid.NewGuid();
        var order = CreatePlacedOrder(customerId);
        order.Cancel("customer-request", "customer", customerId, null, UtcNow);
        _orders.Add(order);
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, customerId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CancellationNotAllowed, result.Error);
    }
}
