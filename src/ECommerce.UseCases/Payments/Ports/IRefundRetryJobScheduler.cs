namespace ECommerce.UseCases.Payments.Ports;

/// <summary>Schedules a failed refund for retry (UC-I-004: max 5 attempts, backoff).</summary>
public interface IRefundRetryJobScheduler
{
    void EnqueueRetry(Guid refundId);
}
