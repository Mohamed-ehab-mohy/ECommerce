namespace ECommerce.Domain.Fulfillment;

public enum FulfillmentTaskStatus
{
    Queued,
    Assigned,
    Picking,
    Packed,
    Shipped,
    Cancelled
}
