namespace ECommerce.Domain.Orders;

public enum CheckoutStatus
{
    Created,
    PaymentAuthorized,
    Placed,
    Expired
}
