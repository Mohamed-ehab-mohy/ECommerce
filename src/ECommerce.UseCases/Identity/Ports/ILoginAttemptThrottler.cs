namespace ECommerce.UseCases.Identity.Ports;

public interface ILoginAttemptThrottler
{
    bool IsBlocked(string clientIp, DateTime utcNow, out int retryAfterSeconds);

    void RecordFailure(string clientIp, DateTime utcNow);

    void RecordSuccess(string clientIp, DateTime utcNow);
}
