using ECommerce.Domain.Common;
using ECommerce.Domain.Events;
using ECommerce.Shared.Primitives;

namespace ECommerce.Domain.Orders;

public sealed class Checkout : BaseEntity<Guid>
{
    private Checkout()
    {
        CustomerEmail = string.Empty;
        Currency = string.Empty;
        ShippingMethodId = string.Empty;
        PriceSnapshot = PriceSnapshot.Empty;
        ShippingAddress = null!;
        BillingAddress = null!;
    }

    public Guid CartId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string CustomerEmail { get; private set; }

    public string Currency { get; private set; }

    public PriceSnapshot PriceSnapshot { get; private set; }

    public AddressSnapshot ShippingAddress { get; private set; }

    public AddressSnapshot BillingAddress { get; private set; }

    public string ShippingMethodId { get; private set; }

    public CheckoutStatus Status { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public Guid? PaymentId { get; private set; }

    public DateTime? PlacedAt { get; private set; }

    public static Checkout Create(
        Guid cartId,
        Guid? customerId,
        string customerEmail,
        string currency,
        PriceSnapshot priceSnapshot,
        AddressSnapshot shippingAddress,
        AddressSnapshot billingAddress,
        string shippingMethodId,
        Guid paymentId,
        DateTime expiresAtUtc,
        DateTime utcNow)
    {
        var checkout = new Checkout
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            CustomerId = customerId,
            CustomerEmail = customerEmail,
            Currency = currency,
            PriceSnapshot = priceSnapshot,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            ShippingMethodId = shippingMethodId,
            Status = CheckoutStatus.Created,
            PaymentId = paymentId,
            ExpiresAt = expiresAtUtc,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        checkout.AddDomainEvent(new CheckoutCreated(checkout.Id, cartId));

        return checkout;
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    public Result MarkPaymentAuthorized(DateTime utcNow)
    {
        if (Status != CheckoutStatus.Created)
        {
            return CheckoutErrors.InvalidState;
        }

        Status = CheckoutStatus.PaymentAuthorized;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result MarkPlaced(DateTime utcNow)
    {
        if (Status != CheckoutStatus.PaymentAuthorized)
        {
            return CheckoutErrors.InvalidState;
        }

        Status = CheckoutStatus.Placed;
        PlacedAt = utcNow;
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public Result Expire(DateTime utcNow)
    {
        if (Status is CheckoutStatus.Placed or CheckoutStatus.Expired)
        {
            return CheckoutErrors.InvalidState;
        }

        Status = CheckoutStatus.Expired;
        UpdatedAt = utcNow;
        return Result.Success();
    }
}
