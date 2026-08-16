using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Integrations;

public static class WebhookErrors
{
    public static readonly Error EndpointNotFound = new(
        "Integrations.EndpointNotFound",
        "The webhook endpoint was not found.",
        ErrorType.NotFound);

    public static readonly Error DeliveryNotFound = new(
        "Integrations.DeliveryNotFound",
        "The webhook delivery was not found.",
        ErrorType.NotFound);
}
