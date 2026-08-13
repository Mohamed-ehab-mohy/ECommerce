namespace ECommerce.UseCases.Payments.Ports;

/// <summary>
/// Health signal for payment providers (US-G-003). Failures (transport errors, explicit
/// provider_unavailable signals) trip a per-provider circuit breaker; business declines do not.
/// </summary>
public interface IPaymentProviderHealth
{
    bool IsAvailable(string providerKey);

    void RecordSuccess(string providerKey);

    void RecordFailure(string providerKey);
}
