namespace ECommerce.Infrastructure.Payments;

public sealed class StripeWebhookOptions
{
    public const string SectionName = "Stripe";

    public string WebhookSecret { get; set; } = string.Empty;
}
