using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Handlers;

namespace ECommerce.UnitTests;

public sealed class RequestRefundCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeOrderRepository _orders = new();

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeRefundRepository _refunds = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m, 0m));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private RequestRefundCommandHandler CreateHandler() =>
        new(
            _orders,
            _payments,
            _refunds,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new RequestRefundCommandValidator());

    private (Order Order, Payment Payment) CreateCapturedOrder()
    {
        var payment = Payment.Create(Guid.NewGuid(), "mock", "mock_tok_1", "tok_client_1", null, "USD", 100.00m, null, UtcNow);
        payment.MarkAuthorized("pi_mock_1_auth", UtcNow);
        payment.Capture(100.00m, UtcNow);

        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            payment.CustomerId,
            "ahmed@example.com",
            "USD",
            "E-20260814-000001",
            Snapshot,
            Address,
            Address,
            "standard",
            payment.Id,
            UtcNow);
        payment.AttachOrder(order.Id, UtcNow);

        _orders.Add(order);
        _payments.Add(payment);
        return (order, payment);
    }

    private static RequestRefundCommand CreateCommand(
        string orderNumber,
        decimal amount,
        string idempotencyKey = "refund-key-1") =>
        new(orderNumber, amount, "item.damaged", null, true, idempotencyKey);

    [Fact]
    public async Task Handle_Creates_Refund_With_Remaining_Refundable()
    {
        var (order, _) = CreateCapturedOrder();
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, 40.00m),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(40.00m, result.Value.Amount);
        Assert.Equal("requested", result.Value.Status);
        Assert.Equal(100.00m, result.Value.RefundableAmount);
        Assert.Equal("refund-key-1", result.Value.IdempotencyKey);
        Assert.Single(_refunds.Refunds);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Amount_Exceeding_Refundable_Is_Rejected()
    {
        var (order, _) = CreateCapturedOrder();
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, 150.00m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefundErrors.ExceedsRefundable, result.Error);
        Assert.Empty(_refunds.Refunds);
    }

    [Fact]
    public async Task Handle_Non_Captured_Payment_Is_Rejected()
    {
        var payment = Payment.Create(Guid.NewGuid(), "mock", "mock_tok_1", "tok_client_1", null, "USD", 100.00m, null, UtcNow);
        payment.MarkAuthorized("pi_mock_1_auth", UtcNow);
        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            payment.CustomerId,
            "ahmed@example.com",
            "USD",
            "E-20260814-000002",
            Snapshot,
            Address,
            Address,
            "standard",
            payment.Id,
            UtcNow);
        _orders.Add(order);
        _payments.Add(payment);
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand(order.OrderNumber, 10.00m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefundErrors.InvalidState, result.Error);
    }

    [Fact]
    public async Task Handle_Missing_Order_Is_Rejected()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            CreateCommand("E-UNKNOWN-000001", 10.00m),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefundErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_Same_Key_Replays_Stored_Refund_Without_Duplicate()
    {
        var (order, _) = CreateCapturedOrder();
        var handler = CreateHandler();

        var first = await handler.Handle(CreateCommand(order.OrderNumber, 40.00m), CancellationToken.None);
        var second = await handler.Handle(CreateCommand(order.OrderNumber, 40.00m), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.RefundId, second.Value.RefundId);
        Assert.Single(_refunds.Refunds);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Same_Key_Different_Order_Is_Rejected()
    {
        var (order, _) = CreateCapturedOrder();
        var payment = Payment.Create(Guid.NewGuid(), "mock", "mock_tok_2", "tok_client_2", null, "USD", 50.00m, null, UtcNow);
        payment.MarkAuthorized("pi_mock_2_auth", UtcNow);
        payment.Capture(50.00m, UtcNow);
        var otherOrder = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            payment.CustomerId,
            "other@example.com",
            "USD",
            "E-20260814-000003",
            Snapshot,
            Address,
            Address,
            "standard",
            payment.Id,
            UtcNow);
        payment.AttachOrder(otherOrder.Id, UtcNow);
        _orders.Add(otherOrder);
        _payments.Add(payment);
        var handler = CreateHandler();

        var first = await handler.Handle(CreateCommand(order.OrderNumber, 40.00m), CancellationToken.None);
        var second = await handler.Handle(CreateCommand(otherOrder.OrderNumber, 40.00m), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(RefundErrors.IdempotencyKeyReuse, second.Error);
    }

    [Fact]
    public async Task Handle_Partial_Refund_Reduces_Refundable()
    {
        var (order, _) = CreateCapturedOrder();
        var handler = CreateHandler();

        await handler.Handle(CreateCommand(order.OrderNumber, 40.00m), CancellationToken.None);
        var second = await handler.Handle(CreateCommand(order.OrderNumber, 40.00m, "refund-key-2"), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(60.00m, second.Value.RefundableAmount);
        Assert.Equal(2, _refunds.Refunds.Count);
    }
}
