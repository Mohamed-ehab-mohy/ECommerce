using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Handlers;
using ECommerce.UseCases.Payments.Options;
using ECommerce.UseCases.Payments.Ports;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests;

public sealed class ExecuteRefundCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeOrderRepository _orders = new();

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeRefundRepository _refunds = new();

    private readonly FakeStockAllocator _allocator = new();

    private readonly FakeAuditLogWriter _audit = new();

    private readonly FakeRefundRetryJobScheduler _retryScheduler = new();

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

    private ExecuteRefundCommandHandler CreateHandler(
        FakePaymentProviderFactory factory,
        RefundRetryOptions? retry = null) =>
        new(
            _refunds,
            _payments,
            _orders,
            factory,
            new FakePaymentProviderHealth(),
            _allocator,
            _retryScheduler,
            _audit,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            Options.Create(retry ?? new RefundRetryOptions()),
            new ExecuteRefundCommandValidator());

    private (Order Order, Payment Payment, Refund Refund) CreateApprovedRefund(bool restock = true)
    {
        var productId = Snapshot.Lines[0].ProductId;
        var payment = Payment.Create(Guid.NewGuid(), "mock", "mock_tok_1", "tok_client_1", "pi_mock_1", "USD", 100.00m, null, UtcNow);
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

        var refund = Refund.Create(
            order.Id,
            payment.Id,
            40.00m,
            "USD",
            "item.damaged",
            restock,
            "refund-key-1",
            [RefundItem.Create(Guid.Empty, productId, 1)],
            UtcNow);
        refund.Approve(Guid.NewGuid(), UtcNow);

        _orders.Add(order);
        _payments.Add(payment);
        _refunds.Add(refund);
        return (order, payment, refund);
    }

    private static ExecuteRefundCommand CreateCommand(Guid refundId) => new(refundId);

    [Fact]
    public async Task Handle_Success_Completes_Refund_And_Marks_Payment_Refunded()
    {
        var (_, payment, refund) = CreateApprovedRefund();
        var handler = CreateHandler(new FakePaymentProviderFactory());

        var result = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("completed", result.Value.Status);
        Assert.NotNull(result.Value.ProviderReference);
        Assert.Equal(RefundStatus.Completed, refund.Status);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Equal(1, _unitOfWork.LastTransaction!.CommitCount);
    }

    [Fact]
    public async Task Handle_Success_With_Restock_Releases_Stock()
    {
        var (_, _, refund) = CreateApprovedRefund(restock: true);
        var handler = CreateHandler(new FakePaymentProviderFactory());

        var result = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _allocator.ReleaseCount);
        Assert.Equal("REFUND", _allocator.LastReason);
        Assert.Equal(refund.Id.ToString("N"), _allocator.LastReference);
        var released = Assert.Single(_allocator.LastItems);
        Assert.Equal("SKU-1", released.Sku);
        Assert.Equal(1, released.Quantity);
    }

    [Fact]
    public async Task Handle_Success_Without_Restock_Does_Not_Release_Stock()
    {
        var (_, _, refund) = CreateApprovedRefund(restock: false);
        var handler = CreateHandler(new FakePaymentProviderFactory());

        var result = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _allocator.ReleaseCount);
    }

    [Fact]
    public async Task Handle_Completed_Refund_Replays_Without_Second_Provider_Call()
    {
        var (_, _, refund) = CreateApprovedRefund();
        var factory = new FakePaymentProviderFactory();
        var handler = CreateHandler(factory);

        var first = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);
        var second = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, factory.Provider.RefundCallCount);
    }

    [Fact]
    public async Task Handle_Uses_Refund_Id_As_Provider_Idempotency_Key()
    {
        var (_, _, refund) = CreateApprovedRefund();
        var factory = new FakePaymentProviderFactory();
        var handler = CreateHandler(factory);

        await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.NotNull(factory.Provider.LastRefundRequest);
        Assert.Equal(refund.Id.ToString("N"), factory.Provider.LastRefundRequest!.IdempotencyKey);
        Assert.Equal("pi_mock_1_auth", factory.Provider.LastRefundRequest!.ProviderReference);
        Assert.Equal(40.00m, factory.Provider.LastRefundRequest!.Amount);
    }

    [Fact]
    public async Task Handle_Requested_Refund_Is_Rejected()
    {
        var (_, _, refund) = CreateApprovedRefund();
        var requested = Refund.Create(
            refund.OrderId,
            refund.PaymentId,
            40.00m,
            "USD",
            "item.damaged",
            false,
            "refund-key-2",
            [],
            UtcNow);
        _refunds.Add(requested);
        var handler = CreateHandler(new FakePaymentProviderFactory());

        var result = await handler.Handle(CreateCommand(requested.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefundErrors.InvalidState, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Provider_Failure_Flags_Refund_And_Schedules_Retry()
    {
        var (_, _, refund) = CreateApprovedRefund();
        var provider = new FakePaymentProvider { RefundResult = new PaymentRefundResult(false, null, "refund_failed") };
        var handler = CreateHandler(new FakePaymentProviderFactory(provider: provider));

        var result = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("failed", result.Value.Status);
        Assert.Equal(RefundStatus.Failed, refund.Status);
        Assert.Equal("refund_failed", refund.FailureDetail);
        Assert.Single(_retryScheduler.Enqueued);
        Assert.Equal(refund.Id, _retryScheduler.Enqueued[0]);
        Assert.Equal(1, provider.RefundCallCount);
    }

    [Fact]
    public async Task Handle_Provider_Failure_Exceeding_Max_Attempts_Does_Not_Schedule_Retry()
    {
        var (_, _, refund) = CreateApprovedRefund();
        var provider = new FakePaymentProvider { RefundResult = new PaymentRefundResult(false, null, "refund_failed") };
        var handler = CreateHandler(new FakePaymentProviderFactory(provider: provider), retry: new RefundRetryOptions { MaxAttempts = 1 });

        var result = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("failed", result.Value.Status);
        Assert.Equal(RefundStatus.Failed, refund.Status);
        Assert.Empty(_retryScheduler.Enqueued);
    }

    [Fact]
    public async Task Handle_Missing_Refund_Is_Rejected()
    {
        var handler = CreateHandler(new FakePaymentProviderFactory());

        var result = await handler.Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RefundErrors.RefundNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_Writes_Audit_Entry_On_Success()
    {
        var (_, _, refund) = CreateApprovedRefund();
        var handler = CreateHandler(new FakePaymentProviderFactory());

        var result = await handler.Handle(CreateCommand(refund.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var operation = Assert.Single(_audit.Operations);
        Assert.Equal("payments.refund.executed", operation.Action);
        Assert.Equal(refund.Id.ToString(), operation.EntityId);
    }
}
