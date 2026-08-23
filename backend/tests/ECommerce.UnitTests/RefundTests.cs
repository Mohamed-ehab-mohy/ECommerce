using ECommerce.Domain.Events;
using ECommerce.Domain.Payments;

namespace ECommerce.UnitTests;

public sealed class RefundTests
{
    private static readonly DateTime Now = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid OrderId = Guid.NewGuid();

    private static readonly Guid PaymentId = Guid.NewGuid();

    private static Refund CreateRefund() =>
        Refund.Create(
            OrderId,
            PaymentId,
            40.00m,
            "USD",
            "item.damaged",
            true,
            "refund-key-1",
            [RefundItem.Create(Guid.Empty, Guid.NewGuid(), 1)],
            Now);

    [Fact]
    public void Create_Sets_Requested_Status_And_Raises_Event()
    {
        var refund = CreateRefund();

        Assert.Equal(RefundStatus.Requested, refund.Status);
        Assert.Equal(40.00m, refund.Amount);
        Assert.Equal(OrderId, refund.OrderId);
        Assert.Equal(PaymentId, refund.PaymentId);
        Assert.True(refund.Restock);
        Assert.Equal(0, refund.Attempts);
        Assert.Single(refund.DomainEvents);
        Assert.IsType<RefundRequested>(refund.DomainEvents.First());
    }

    [Fact]
    public void Approve_Transitions_To_Approved_And_Raises_Event()
    {
        var refund = CreateRefund();

        var result = refund.Approve(Guid.NewGuid(), Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(RefundStatus.Approved, refund.Status);
        Assert.NotNull(refund.ApprovedAt);
        Assert.IsType<RefundApproved>(refund.DomainEvents.Last());
    }

    [Fact]
    public void Reject_Transitions_To_Rejected()
    {
        var refund = CreateRefund();

        var result = refund.Reject(Guid.NewGuid(), "policy", Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(RefundStatus.Rejected, refund.Status);
        Assert.Equal("policy", refund.FailureDetail);
        Assert.IsType<RefundRejected>(refund.DomainEvents.Last());
    }

    [Fact]
    public void Approve_On_Non_Requested_Refund_Is_Rejected()
    {
        var refund = CreateRefund();
        refund.Approve(Guid.NewGuid(), Now);

        var result = refund.Approve(Guid.NewGuid(), Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(RefundErrors.InvalidState, result.Error);
    }

    [Fact]
    public void BeginExecution_Increments_Attempts_And_Raises_Event()
    {
        var refund = CreateRefund();
        refund.Approve(Guid.NewGuid(), Now);

        var result = refund.BeginExecution(Now.AddMinutes(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(RefundStatus.Executing, refund.Status);
        Assert.Equal(1, refund.Attempts);
        Assert.IsType<RefundExecuting>(refund.DomainEvents.Last());
    }

    [Fact]
    public void BeginExecution_On_Requested_Refund_Is_Rejected()
    {
        var refund = CreateRefund();

        var result = refund.BeginExecution(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(RefundErrors.InvalidState, result.Error);
    }

    [Fact]
    public void MarkCompleted_Transitions_To_Completed_And_Raises_Event()
    {
        var refund = CreateRefund();
        refund.Approve(Guid.NewGuid(), Now);
        refund.BeginExecution(Now.AddMinutes(2));

        var result = refund.MarkCompleted("mock_refund_123", Now.AddMinutes(3));

        Assert.True(result.IsSuccess);
        Assert.Equal(RefundStatus.Completed, refund.Status);
        Assert.Equal("mock_refund_123", refund.ProviderReference);
        Assert.IsType<RefundCompleted>(refund.DomainEvents.Last());
    }

    [Fact]
    public void MarkFailed_Transitions_To_Failed_And_Raises_Event()
    {
        var refund = CreateRefund();
        refund.Approve(Guid.NewGuid(), Now);
        refund.BeginExecution(Now.AddMinutes(2));

        var result = refund.MarkFailed("refund_failed", Now.AddMinutes(3));

        Assert.True(result.IsSuccess);
        Assert.Equal(RefundStatus.Failed, refund.Status);
        Assert.Equal("refund_failed", refund.FailureDetail);
        Assert.IsType<RefundFailed>(refund.DomainEvents.Last());
    }

    [Fact]
    public void Failed_Refund_Can_Retry()
    {
        var refund = CreateRefund();
        refund.Approve(Guid.NewGuid(), Now);
        refund.BeginExecution(Now.AddMinutes(2));
        refund.MarkFailed("refund_failed", Now.AddMinutes(3));

        var retry = refund.BeginExecution(Now.AddMinutes(4));

        Assert.True(retry.IsSuccess);
        Assert.Equal(2, refund.Attempts);
        Assert.Equal(RefundStatus.Executing, refund.Status);
    }
}
