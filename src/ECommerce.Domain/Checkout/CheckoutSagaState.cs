using ECommerce.Domain.Common;
using MassTransit;

namespace ECommerce.Domain.Checkout;

public sealed class CheckoutSagaState : BaseEntity<Guid>, SagaStateMachineInstance
{
    private CheckoutSagaState() { }

    public Guid CorrelationId { get; set; }
    public string? CurrentState { get; set; }

    public Guid CheckoutId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? CustomerId { get; set; }

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? IdempotencyKey { get; set; }

    public static CheckoutSagaState Create(Guid checkoutId, Guid? customerId, string idempotencyKey, DateTime utcNow)
    {
        var id = Guid.NewGuid();
        return new CheckoutSagaState
        {
            Id = id,
            CorrelationId = id,
            CheckoutId = checkoutId,
            CustomerId = customerId,
            CurrentState = "Initiated",
            IdempotencyKey = idempotencyKey,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void TransitionTo(string state, DateTime utcNow)
    {
        CurrentState = state;
        UpdatedAt = utcNow;
    }

    public void SetError(string error, DateTime utcNow)
    {
        ErrorMessage = error;
        UpdatedAt = utcNow;
    }

    public void IncrementRetry(DateTime utcNow)
    {
        RetryCount++;
        UpdatedAt = utcNow;
    }
}
