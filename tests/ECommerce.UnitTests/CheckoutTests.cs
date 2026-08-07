using ECommerce.Domain.Orders;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UnitTests;

public sealed class CheckoutTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m));

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static CheckoutAggregate CreateCheckout() =>
        CheckoutAggregate.Create(
            Guid.NewGuid(),
            null,
            "ahmed@example.com",
            "AED",
            Snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            Now.AddMinutes(30),
            Now);

    [Fact]
    public void Create_Sets_Created_Status_And_Domain_Event()
    {
        var checkout = CreateCheckout();

        Assert.Equal(CheckoutStatus.Created, checkout.Status);
        Assert.Equal("ahmed@example.com", checkout.CustomerEmail);
        Assert.Equal(30.00m, checkout.PriceSnapshot.Totals.Subtotal);
        Assert.Single(checkout.DomainEvents);
    }

    [Fact]
    public void IsExpired_Returns_True_After_Ttl()
    {
        var checkout = CreateCheckout();

        Assert.True(checkout.IsExpired(Now.AddMinutes(31)));
        Assert.False(checkout.IsExpired(Now.AddMinutes(29)));
    }

    [Fact]
    public void MarkPaymentAuthorized_Transitions_To_PaymentAuthorized()
    {
        var checkout = CreateCheckout();

        var result = checkout.MarkPaymentAuthorized(Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(CheckoutStatus.PaymentAuthorized, checkout.Status);
    }

    [Fact]
    public void MarkPaymentAuthorized_From_Non_Created_Is_Rejected()
    {
        var checkout = CreateCheckout();
        checkout.MarkPaymentAuthorized(Now);

        var result = checkout.MarkPaymentAuthorized(Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.InvalidState, result.Error);
    }

    [Fact]
    public void MarkPlaced_Requires_Payment_Authorized()
    {
        var checkout = CreateCheckout();

        var result = checkout.MarkPlaced(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.InvalidState, result.Error);
    }

    [Fact]
    public void MarkPlaced_After_Authorization_Sets_Placed()
    {
        var checkout = CreateCheckout();
        checkout.MarkPaymentAuthorized(Now);

        var result = checkout.MarkPlaced(Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(CheckoutStatus.Placed, checkout.Status);
        Assert.NotNull(checkout.PlacedAt);
    }

    [Fact]
    public void Expire_Sets_Expired_From_Created()
    {
        var checkout = CreateCheckout();

        var result = checkout.Expire(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(CheckoutStatus.Expired, checkout.Status);
    }

    [Fact]
    public void Expire_Placed_Checkout_Is_Rejected()
    {
        var checkout = CreateCheckout();
        checkout.MarkPaymentAuthorized(Now);
        checkout.MarkPlaced(Now);

        var result = checkout.Expire(Now);

        Assert.True(result.IsFailure);
        Assert.Equal(CheckoutErrors.InvalidState, result.Error);
    }
}
