
namespace ECommerce.Domain.Payments;

public static class RefundErrors
{
    public static readonly Error RefundNotFound = new(
        "ERR_PAY_008",
        "The refund was not found.",
        ErrorType.NotFound);

    /// <summary>Refund amount must not exceed the paid amount minus already refunded amounts (BR-1606, RF-1).</summary>
    public static readonly Error ExceedsRefundable = new(
        "ERR_PAY_003",
        "The refund amount exceeds the refundable amount for this payment.",
        ErrorType.Conflict);

    public static readonly Error InvalidState = new(
        "ERR_PAY_002",
        "The refund state does not allow this operation.",
        ErrorType.Conflict);

    public static readonly Error NotApproved = new(
        "ERR_PAY_011",
        "The refund must be approved before it can be executed.",
        ErrorType.Conflict);

    public static readonly Error IdempotencyKeyReuse = new(
        "ERR_PAY_009",
        "The refund idempotency key was already used for a different order.",
        ErrorType.Conflict);

    public static readonly Error OrderNotFound = new(
        "ERR_RES_002",
        "The order was not found.",
        ErrorType.NotFound);
}
