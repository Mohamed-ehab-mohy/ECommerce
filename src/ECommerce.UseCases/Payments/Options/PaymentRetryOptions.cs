namespace ECommerce.UseCases.Payments.Options;

/// <summary>Bounded-retry policy for declined payments (US-G-004). Bound under <c>Payments:Retry</c>.</summary>
public sealed class PaymentRetryOptions
{
    public const string SectionName = "Payments:Retry";

    /// <summary>Maximum total authorization attempts before the payment settles as failed.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Minimum wait between a decline and its next retry.</summary>
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(30);
}
