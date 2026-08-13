namespace ECommerce.Domain.Orders;

public enum OrderStatus
{
    Pending,
    Placed,
    AwaitingPayment,
    Paid,
    Backordered,
    AwaitingFulfillment,
    Picking,
    Packed,
    Shipped,
    Delivered,
    Completed,
    Cancelled
}
