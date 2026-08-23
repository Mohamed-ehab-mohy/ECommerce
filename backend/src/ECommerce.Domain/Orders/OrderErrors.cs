using ECommerce.Domain.Events;

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

    public static readonly Error NotYourOrder = new(
        "ERR_ORD_003",
        "The order does not belong to the current customer.",
        ErrorType.Forbidden);

    public static readonly Error CancellationNotAllowed = new(
        "ERR_ORD_004",
        "The order cannot be cancelled in its current state.",
        ErrorType.Conflict);

    public static readonly Error BackorderAlreadyOpen = new(
        "ERR_ORD_005",
        "The order already has an open backorder for this product.",
        ErrorType.Conflict);

    public static readonly Error IdempotencyKeyReuse = new(
        "ERR_IDP_001",
        "The idempotency key was already used for a different checkout.",
        ErrorType.Conflict);

    public static readonly Error AddressCorrectionNotAllowed = new(
        "ERR_ORD_006",
        "The shipping address can only be corrected before the order ships.",
        ErrorType.Conflict);
}
