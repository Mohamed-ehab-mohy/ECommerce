using ECommerce.Domain.Payments;

namespace ECommerce.UnitTests;

public sealed class PaymentTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static Payment CreatePayment(decimal amount = 100m) =>
        Payment.Create(
            Guid.NewGuid(),
            "mock",
            "mock_tok_123",
            "tok_123",
            "mock_intent_123",
            "USD",
            amount,
            null,
            Now);

    [Fact]
    public void Create_Sets_Created_Status_And_No_Authorized_Amount()
    {
        var payment = CreatePayment();

        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Equal(0m, payment.AuthorizedAmount);
        Assert.Null(payment.AuthorizedAt);
    }

    [Fact]
    public void MarkAuthorized_Sets_Status_Reference_And_Amount()
    {
        var payment = CreatePayment();

        var result = payment.MarkAuthorized("mock_auth_1", Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal(100m, payment.AuthorizedAmount);
        Assert.Equal("mock_auth_1", payment.ProviderReference);
        Assert.Equal(Now.AddMinutes(1), payment.AuthorizedAt);
    }

    [Fact]
    public void MarkAuthorized_On_Authorized_Payment_Is_Rejected()
    {
        var payment = CreatePayment();
        payment.MarkAuthorized("mock_auth_1", Now);

        var result = payment.MarkAuthorized("mock_auth_2", Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.CaptureConflict, result.Error);
    }

    [Fact]
    public void Capture_Le_Authorized_Amount_Succeeds()
    {
        var payment = CreatePayment();
        payment.MarkAuthorized("mock_auth_1", Now);

        var result = payment.Capture(80m, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Captured, payment.Status);
        Assert.NotNull(payment.CapturedAt);
    }

    [Fact]
    public void Capture_Exceeding_Authorization_Is_Rejected()
    {
        var payment = CreatePayment();
        payment.MarkAuthorized("mock_auth_1", Now);

        var result = payment.Capture(120m, Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.CaptureExceedsAuthorization, result.Error);
    }

    [Fact]
    public void Capture_Without_Authorization_Is_Rejected()
    {
        var payment = CreatePayment();

        var result = payment.Capture(50m, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.CaptureConflict, result.Error);
    }

    [Fact]
    public void Void_Authorized_Payment_Cancels_It()
    {
        var payment = CreatePayment();
        payment.MarkAuthorized("mock_auth_1", Now);

        var result = payment.Void(Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        Assert.NotNull(payment.VoidedAt);
    }

    [Fact]
    public void RecordAttempt_Appends_Append_Only_Ledger()
    {
        var payment = CreatePayment();

        payment.RecordAttempt("authorize", 100m, "success", "{\"id\":\"pi_1\"}", "trace-1", Now);

        Assert.Equal(1, payment.Attempt);
        var attempt = Assert.Single(payment.Attempts);
        Assert.Equal(1, attempt.AttemptNo);
        Assert.Equal("authorize", attempt.Action);
        Assert.Equal("trace-1", attempt.TraceId);
    }

    [Fact]
    public void AttachOrder_Links_Order_Once()
    {
        var payment = CreatePayment();
        var orderId = Guid.NewGuid();

        Assert.True(payment.AttachOrder(orderId, Now).IsSuccess);
        Assert.True(payment.AttachOrder(orderId, Now).IsSuccess);
        Assert.Equal(orderId, payment.OrderId);
    }
}
