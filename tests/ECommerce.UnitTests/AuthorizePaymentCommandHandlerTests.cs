using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Payments.Commands;
using ECommerce.UseCases.Payments.Handlers;
using System.Text.Json;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UnitTests;

public sealed class AuthorizePaymentCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakePaymentRepository _payments = new();

    private readonly FakeCheckoutRepository _checkouts = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m));

    private (Payment Payment, CheckoutAggregate Checkout) CreateCheckoutPayment()
    {
        var payment = Payment.Create(
            null, "mock", "mock_tok_1", "tok_client_1", "mock_intent_1", "USD", 39.90m, null, UtcNow);
        var checkout = CheckoutAggregate.Create(
            Guid.NewGuid(), null, "ahmed@example.com", "USD", Snapshot, Address, Address, "standard",
            payment.Id, UtcNow.AddMinutes(30), UtcNow);

        _payments.Add(payment);
        _checkouts.Add(checkout);
        return (payment, checkout);
    }

    private AuthorizePaymentCommandHandler CreateHandler(FakePaymentProviderFactory factory) =>
        new(
            _payments,
            _checkouts,
            factory,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new AuthorizePaymentCommandValidator());

    private static AuthorizePaymentCommand CreateCommand(Guid paymentId) => new(paymentId);

    [Fact]
    public async Task Authorize_Success_Authorizes_Payment_And_Checkout()
    {
        var (payment, checkout) = CreateCheckoutPayment();
        var factory = new FakePaymentProviderFactory();

        var result = await CreateHandler(factory).Handle(CreateCommand(payment.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(payment.Id, result.Value.PaymentId);
        Assert.Equal(PaymentStatus.Authorized, result.Value.Status);
        Assert.Equal("pi_mock_1_auth", result.Value.ProviderReference);
        Assert.Equal("tok_client_1", result.Value.ClientToken);

        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.NotNull(payment.AuthorizedAt);
        Assert.Equal(1, payment.Attempt);
        var attempt = Assert.Single(payment.Attempts);
        Assert.Equal("authorize", attempt.Action);
        Assert.Equal("authorized", attempt.Status);
        var response = JsonSerializer.Deserialize<PaymentAuthorizationResult>(attempt.ProviderResponse!);
        Assert.NotNull(response);
        Assert.Equal("pi_mock_1_auth", response.ProviderReference);
        Assert.Null(response.DeclineCode);

        Assert.Equal(CheckoutStatus.PaymentAuthorized, checkout.Status);

        Assert.Equal(39.90m, factory.Provider.LastAuthorizationRequest!.Amount);
        Assert.Equal("USD", factory.Provider.LastAuthorizationRequest.Currency);
        Assert.Equal("mock_tok_1", factory.Provider.LastAuthorizationRequest.ProviderToken);

        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.NotNull(_unitOfWork.LastTransaction);
        Assert.Equal(1, _unitOfWork.LastTransaction.CommitCount);
    }

    [Fact]
    public async Task Authorize_Declined_Returns_402_With_Customer_Message()
    {
        var (payment, _) = CreateCheckoutPayment();
        var provider = new FakePaymentProvider(
            "mock",
            authorization: new PaymentAuthorizationResult(false, string.Empty, "card_declined"));
        var factory = new FakePaymentProviderFactory("mock", provider);

        var result = await CreateHandler(factory).Handle(CreateCommand(payment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.PaymentDeclined, result.Error);
        Assert.Equal(ErrorType.PaymentRequired, result.Error.Type);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(1, payment.Attempt);
        var attempt = Assert.Single(payment.Attempts);
        Assert.Equal("declined", attempt.Status);
        var response = JsonSerializer.Deserialize<PaymentAuthorizationResult>(attempt.ProviderResponse!);
        Assert.NotNull(response);
        Assert.Equal("card_declined", response.DeclineCode);
    }

    [Fact]
    public async Task Authorize_Provider_Unavailable_Returns_BadGateway()
    {
        var (payment, _) = CreateCheckoutPayment();
        var factory = new FakePaymentProviderFactory();
        factory.MissingKey = "mock";

        var result = await CreateHandler(factory).Handle(CreateCommand(payment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.ProviderUnavailable, result.Error);
        Assert.Equal(ErrorType.BadGateway, result.Error.Type);
        Assert.Equal(PaymentStatus.Created, payment.Status);
        Assert.Equal(0, payment.Attempt);
    }

    [Fact]
    public async Task Authorize_Provider_Unavailable_DeclineCode_Returns_BadGateway()
    {
        var (payment, _) = CreateCheckoutPayment();
        var provider = new FakePaymentProvider(
            "mock",
            authorization: new PaymentAuthorizationResult(false, string.Empty, "provider_unavailable"));
        var factory = new FakePaymentProviderFactory("mock", provider);

        var result = await CreateHandler(factory).Handle(CreateCommand(payment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.ProviderUnavailable, result.Error);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public async Task Authorize_Already_Authorized_Is_Idempotent()
    {
        var (payment, checkout) = CreateCheckoutPayment();
        payment.MarkAuthorized("pi_auth_1", UtcNow);
        var factory = new FakePaymentProviderFactory();

        var result = await CreateHandler(factory).Handle(CreateCommand(payment.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Authorized, result.Value.Status);
        Assert.Equal("pi_auth_1", result.Value.ProviderReference);
        Assert.Equal(0, payment.Attempt);
        Assert.Equal(CheckoutStatus.PaymentAuthorized, checkout.Status);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Authorize_Unknown_Payment_Returns_NotFound()
    {
        var factory = new FakePaymentProviderFactory();

        var result = await CreateHandler(factory).Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.PaymentNotFound, result.Error);
    }

    [Fact]
    public async Task Authorize_Captured_Payment_Returns_Conflict()
    {
        var (payment, _) = CreateCheckoutPayment();
        payment.MarkAuthorized("pi_auth_1", UtcNow);
        payment.Capture(39.90m, UtcNow);
        var factory = new FakePaymentProviderFactory();

        var result = await CreateHandler(factory).Handle(CreateCommand(payment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.CaptureConflict, result.Error);
        Assert.Equal(PaymentStatus.Captured, payment.Status);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
