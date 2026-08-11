namespace ECommerce.API.Controllers;

public sealed record CreateCouponRequest(
    string Code,
    Guid PromotionId,
    int TotalUses,
    int? PerCustomerLimit,
    DateTime? StartsAt,
    DateTime? EndsAt);
