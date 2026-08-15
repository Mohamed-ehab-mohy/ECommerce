namespace ECommerce.UseCases.Payments.Options;

/// <summary>Retry budget for failed refund execution (UC-I-004: max 5 attempts). Bound under <c>Payments:Refund</c>.</summary>
public sealed class RefundRetryOptions
{
    public const string SectionName = "Payments:Refund";

    /// <summary>Maximum refund execution attempts (including the initial one) before the refund stays failed.</summary>
    public int MaxAttempts { get; set; } = 5;
}
