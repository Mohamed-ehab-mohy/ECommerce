using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Payments;

public static class PaymentErrors
{
    public static readonly Error PaymentNotFound = new(
        "ERR_RES_001",
        "The payment was not found.",
        ErrorType.NotFound);

    public static readonly Error PaymentDeclined = new(
        "ERR_PAY_001",
        "Your payment was declined. Please use another card or payment method.",
        ErrorType.PaymentRequired);

    public static readonly Error CaptureConflict = new(
        "ERR_PAY_002",
        "The payment state does not allow this operation.",
        ErrorType.Conflict);

    public static readonly Error CaptureExceedsAuthorization = new(
        "ERR_PAY_002",
        "The capture amount must not exceed the authorized amount.",
        ErrorType.Conflict);

    public static readonly Error PaymentNotAuthorized = new(
        "ERR_PAY_003",
        "The payment has not been authorized yet.",
        ErrorType.Conflict);

    public static readonly Error ProviderUnavailable = new(
        "ERR_PAY_004",
        "The payment provider is temporarily unavailable. Please try again later.",
        ErrorType.BadGateway);

    public static readonly Error InvalidSignature = new(
        "ERR_WEB_001",
        "The webhook signature is invalid.",
        ErrorType.Unauthorized);
}
