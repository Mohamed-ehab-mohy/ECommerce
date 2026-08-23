
namespace ECommerce.Domain.Pricing;

public static class PromotionErrors
{
    public static readonly Error NameRequired = new(
        "ERR_PROMO_001",
        "A promotion name is required.",
        ErrorType.Validation);

    public static readonly Error ActionsRequired = new(
        "ERR_PROMO_002",
        "A promotion must define at least one discount action.",
        ErrorType.Validation);

    public static readonly Error InvalidDiscountValue = new(
        "ERR_PROMO_003",
        "A discount value must be greater than zero and at most 100 for percentages.",
        ErrorType.Validation);

    public static readonly Error InvalidDiscountCap = new(
        "ERR_PROMO_004",
        "A discount cap cannot be negative.",
        ErrorType.Validation);

    public static readonly Error InvalidSchedule = new(
        "ERR_PROMO_005",
        "The promotion start must be before its end.",
        ErrorType.Validation);

    public static readonly Error InvalidState = new(
        "ERR_PROMO_006",
        "The promotion state does not allow this operation.",
        ErrorType.Conflict);

    public static readonly Error PromotionNotFound = new(
        "ERR_RES_001",
        "The promotion was not found.",
        ErrorType.NotFound);
}

public static class CouponErrors
{
    public static readonly Error CodeRequired = new(
        "ERR_CPN_001",
        "A coupon code is required.",
        ErrorType.Validation);

    public static readonly Error InvalidTotalUses = new(
        "ERR_CPN_002",
        "A coupon must allow at least one use.",
        ErrorType.Validation);

    public static readonly Error InvalidPerCustomerLimit = new(
        "ERR_CPN_003",
        "The per-customer limit must be at least one.",
        ErrorType.Validation);

    public static readonly Error InvalidSchedule = new(
        "ERR_CPN_004",
        "The coupon start must be before its end.",
        ErrorType.Validation);

    public static readonly Error Exhausted = new(
        "COUPON_EXHAUSTED",
        "This coupon has reached its usage limit.",
        ErrorType.Conflict);

    public static readonly Error AlreadyUsed = new(
        "COUPON_ALREADY_USED",
        "This coupon has already been used by this customer.",
        ErrorType.Conflict);

    public static readonly Error CouponNotFound = new(
        "ERR_RES_001",
        "The coupon was not found.",
        ErrorType.NotFound);

    public static readonly Error Inactive = new(
        "COUPON_INACTIVE",
        "This coupon is not active.",
        ErrorType.Conflict);

    public static readonly Error CustomerRequired = new(
        "COUPON_CUSTOMER_REQUIRED",
        "A coupon can only be applied to a signed-in cart.",
        ErrorType.Forbidden);

    public static readonly Error NotApplied = new(
        "COUPON_NOT_APPLIED",
        "The cart does not have this coupon applied.",
        ErrorType.Conflict);
}

public static class PricingErrors
{
    public static readonly Error PromotionMismatch = new(
        "ERR_PRICING_001",
        "The coupon's promotion is not available for this cart.",
        ErrorType.Conflict);
}
