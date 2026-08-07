namespace ECommerce.Domain.Payments;

public enum PaymentStatus
{
    Created,
    Authorized,
    Failed,
    Captured,
    Cancelled,
    Refunding,
    Refunded,
    RefundFailed
}
