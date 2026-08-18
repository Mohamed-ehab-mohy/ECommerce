using ECommerce.Domain.Checkout;
using ECommerce.UseCases.Messaging.Events;
using MassTransit;

namespace ECommerce.Infrastructure.Messaging;

public sealed class CheckoutSagaStateMachine : MassTransitStateMachine<CheckoutSagaState>
{
    public CheckoutSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => CheckoutInitiatedEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => OrderPlacedFromCheckoutEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => PaymentAuthorizedFromCheckoutEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => PaymentFailedFromCheckoutEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => InventoryReservedFromCheckoutEvent, x => x.CorrelateById(m => m.Message.CheckoutId));
        Event(() => InventoryFailedFromCheckoutEvent, x => x.CorrelateById(m => m.Message.CheckoutId));

        Initially(
            When(CheckoutInitiatedEvent)
                .Then(context =>
                {
                    var now = DateTime.UtcNow;
                    context.Saga.CorrelationId = context.Message.CheckoutId;
                    context.Saga.CheckoutId = context.Message.CheckoutId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.IdempotencyKey = context.Message.IdempotencyKey;
                    context.Saga.TransitionTo("OrderCreated", now);
                })
                .TransitionTo(OrderCreated));

        During(OrderCreated,
            When(OrderPlacedFromCheckoutEvent)
                .Then(context =>
                {
                    var now = DateTime.UtcNow;
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.TransitionTo("PaymentAuthorized", now);
                })
                .TransitionTo(PaymentAuthorized),
            When(PaymentFailedFromCheckoutEvent)
                .Then(context =>
                {
                    var now = DateTime.UtcNow;
                    context.Saga.SetError(context.Message.Reason, now);
                })
                .Publish(context => new CheckoutCompensated(context.Saga.CheckoutId, context.Message.Reason))
                .TransitionTo(Failed));

        During(PaymentAuthorized,
            When(PaymentAuthorizedFromCheckoutEvent)
                .Then(context =>
                {
                    var now = DateTime.UtcNow;
                    context.Saga.PaymentId = context.Message.PaymentId;
                    context.Saga.TransitionTo("InventoryReserved", now);
                })
                .TransitionTo(InventoryReserved),
            When(PaymentFailedFromCheckoutEvent)
                .Then(context =>
                {
                    var now = DateTime.UtcNow;
                    context.Saga.SetError(context.Message.Reason, now);
                })
                .Publish(context => new CheckoutCompensated(context.Saga.CheckoutId, context.Message.Reason))
                .TransitionTo(Failed));

        During(InventoryReserved,
            When(InventoryReservedFromCheckoutEvent)
                .Then(context =>
                {
                    var now = DateTime.UtcNow;
                    context.Saga.TransitionTo("Completed", now);
                })
                .Publish(context => new CheckoutCompleted(
                    context.Saga.CheckoutId,
                    context.Saga.OrderId!.Value,
                    context.Saga.PaymentId!.Value))
                .TransitionTo(Completed),
            When(InventoryFailedFromCheckoutEvent)
                .Then(context =>
                {
                    var now = DateTime.UtcNow;
                    context.Saga.SetError(context.Message.Reason, now);
                })
                .Publish(context => new CheckoutCompensated(context.Saga.CheckoutId, context.Message.Reason))
                .TransitionTo(Failed));

        SetCompletedWhenFinalized();
    }

    public State OrderCreated { get; } = null!;
    public State PaymentAuthorized { get; } = null!;
    public State InventoryReserved { get; } = null!;
    public State Completed { get; } = null!;
    public State Failed { get; } = null!;
    public State Compensating { get; } = null!;

    public Event<CheckoutInitiated> CheckoutInitiatedEvent { get; } = null!;
    public Event<OrderPlacedFromCheckout> OrderPlacedFromCheckoutEvent { get; } = null!;
    public Event<PaymentAuthorizedFromCheckout> PaymentAuthorizedFromCheckoutEvent { get; } = null!;
    public Event<PaymentFailedFromCheckout> PaymentFailedFromCheckoutEvent { get; } = null!;
    public Event<InventoryReservedFromCheckout> InventoryReservedFromCheckoutEvent { get; } = null!;
    public Event<InventoryFailedFromCheckout> InventoryFailedFromCheckoutEvent { get; } = null!;
}
