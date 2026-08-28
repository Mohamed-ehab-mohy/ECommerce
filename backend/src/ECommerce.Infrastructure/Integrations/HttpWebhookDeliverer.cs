using System.Text;
using ECommerce.UseCases.Integrations.Ports;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Integrations;

/// <summary>POSTs a signed payload to a partner endpoint with the X-* delivery headers.</summary>
public sealed class HttpWebhookDeliverer(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpWebhookDeliverer> logger) : IWebhookHttpDeliverer
{
    public async Task<WebhookDeliveryResult> PostAsync(
        string url,
        string signature,
        string eventId,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("webhooks");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("X-Signature", signature);
        request.Headers.TryAddWithoutValidation("X-Event-Id", eventId);
        request.Headers.TryAddWithoutValidation("X-Event-Type", eventType);
        request.Headers.TryAddWithoutValidation("X-Timestamp", DateTimeOffset.UtcNow.ToString("O"));

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;
            var isSuccess = response.IsSuccessStatusCode;
            var body = isSuccess ? null : await response.Content.ReadAsStringAsync(cancellationToken);

            return new WebhookDeliveryResult(isSuccess, statusCode, isSuccess ? null : $"HTTP {statusCode}: {Truncate(body, 500)}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Webhook POST to {Url} timed out.", url);
            return new WebhookDeliveryResult(false, null, "Request timed out.");
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Webhook POST to {Url} failed.", url);
            return new WebhookDeliveryResult(false, null, exception.Message);
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
}
