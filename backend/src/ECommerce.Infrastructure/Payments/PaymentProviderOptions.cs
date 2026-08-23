namespace ECommerce.Infrastructure.Payments;

public sealed class PaymentProviderOptions
{
    public const string SectionName = "Payments";

    public string DefaultProvider { get; set; } = "mock";

    /// <summary>Backup PSP key used when the primary provider's circuit is open (US-G-003).</summary>
    public string FailoverProvider { get; set; } = string.Empty;

    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    public StripeProviderOptions Stripe { get; set; } = new();
}

public sealed class CircuitBreakerOptions
{
    /// <summary>Consecutive failures before the circuit opens.</summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>How long the circuit stays open before allowing a half-open trial.</summary>
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(60);
}

public sealed class StripeProviderOptions
{
    public bool Enabled { get; set; }

    public string SecretKey { get; set; } = string.Empty;
}
