namespace ECommerce.Domain.Orders;

public enum OrderStatus
{
    Pending,
    Placed,
    AwaitingPayment,
    Paid,
    AwaitingFulfillment,
    Picking,
    Packed,
    Shipped,
    Delivered,
    Completed,
    Cancelled
}
