namespace ECommerce.Infrastructure.Payments;

public sealed class PaymentProviderOptions
{
    public const string SectionName = "Payments";

    public string DefaultProvider { get; set; } = "mock";

    public StripeProviderOptions Stripe { get; set; } = new();
}

public sealed class StripeProviderOptions
{
    public bool Enabled { get; set; }

    public string SecretKey { get; set; } = string.Empty;
}
