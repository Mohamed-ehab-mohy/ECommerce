using ECommerce.Domain.Events;
using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error OrderNotFound = new(
        "ERR_ORD_001",
        "The order was not found.",
        ErrorType.NotFound);

    public static readonly Error InvalidState = new(
        "ERR_ORD_002",
        "The order state does not allow this operation.",
        ErrorType.Conflict);

    public static readonly Error IdempotencyKeyReuse = new(
        "ERR_IDP_001",
        "The idempotency key was already used for a different checkout.",
        ErrorType.Conflict);
}
