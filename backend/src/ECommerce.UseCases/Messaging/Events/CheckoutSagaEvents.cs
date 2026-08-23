namespace ECommerce.UseCases.Messaging.Events;

public sealed record CheckoutInitiated(Guid CheckoutId, Guid? CustomerId, string IdempotencyKey);
public sealed record OrderPlacedFromCheckout(Guid CheckoutId, Guid OrderId, decimal Amount, string Currency);
public sealed record PaymentAuthorizedFromCheckout(Guid CheckoutId, Guid OrderId, Guid PaymentId);
public sealed record PaymentFailedFromCheckout(Guid CheckoutId, Guid OrderId, string Reason);
public sealed record InventoryReservedFromCheckout(Guid CheckoutId, Guid OrderId);
public sealed record InventoryFailedFromCheckout(Guid CheckoutId, Guid OrderId, string Reason);
public sealed record CheckoutCompleted(Guid CheckoutId, Guid OrderId, Guid PaymentId);
public sealed record CheckoutCompensated(Guid CheckoutId, string Reason);
public sealed record CheckoutFailed(Guid CheckoutId, string Reason);
