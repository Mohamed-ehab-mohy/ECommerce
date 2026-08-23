namespace ECommerce.Domain.Payments;

public enum PaymentStatus
{
    Created,
    Authorized,
    Failed,
    RetryPending,
    Captured,
    Cancelled,
    Refunding,
    Refunded,
    RefundFailed
}
