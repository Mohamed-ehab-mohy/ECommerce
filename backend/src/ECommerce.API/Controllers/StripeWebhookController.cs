using System.Text.Json;
using ECommerce.Infrastructure.Payments;
using ECommerce.UseCases.Payments.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/webhooks/stripe")]
public sealed class StripeWebhookController(
    ISender sender,
    IOptions<StripeWebhookOptions> webhookOptions,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    /// <summary>Receive and process Stripe webhook events (payments, refunds).</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> HandleStripeEvent(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
        {
            logger.LogWarning("Stripe webhook received without signature header");
            return Unauthorized();
        }

        if (!StripeSignatureVerifier.Verify(rawBody, signatureHeader.ToString(), webhookOptions.Value.WebhookSecret))
        {
            logger.LogWarning("Stripe webhook signature verification failed");
            return Unauthorized();
        }

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;

        var eventType = root.GetProperty("type").GetString();
        if (string.IsNullOrEmpty(eventType))
        {
            return Ok();
        }

        logger.LogInformation("Processing Stripe webhook event: {EventType}", eventType);

        var dataObject = root.GetProperty("data").GetProperty("object");

        try
        {
            switch (eventType)
            {
                case "payment_intent.succeeded":
                    await HandlePaymentIntentSucceededAsync(dataObject, cancellationToken);
                    break;
                case "payment_intent.payment_failed":
                    await HandlePaymentIntentFailedAsync(dataObject, cancellationToken);
                    break;
                case "charge.refunded":
                    await HandleChargeRefundedAsync(dataObject, cancellationToken);
                    break;
                default:
                    logger.LogInformation("Unhandled Stripe event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Stripe webhook event: {EventType}", eventType);
        }

        return Ok();
    }

    private async Task HandlePaymentIntentSucceededAsync(JsonElement dataObject, CancellationToken cancellationToken)
    {
        var paymentIntentId = dataObject.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(paymentIntentId)) return;

        await sender.Send(new HandleStripePaymentSucceededCommand(paymentIntentId), cancellationToken);
        logger.LogInformation("Payment captured via Stripe webhook (PI: {PaymentIntentId})", paymentIntentId);
    }

    private async Task HandlePaymentIntentFailedAsync(JsonElement dataObject, CancellationToken cancellationToken)
    {
        var paymentIntentId = dataObject.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(paymentIntentId)) return;

        await sender.Send(new HandleStripePaymentFailedCommand(paymentIntentId), cancellationToken);
        logger.LogInformation("Payment marked as failed via Stripe webhook (PI: {PaymentIntentId})", paymentIntentId);
    }

    private async Task HandleChargeRefundedAsync(JsonElement dataObject, CancellationToken cancellationToken)
    {
        var paymentIntentId = dataObject.TryGetProperty("payment_intent", out var piProp)
            ? piProp.GetString()
            : null;

        if (string.IsNullOrEmpty(paymentIntentId)) return;

        var amountRefunded = dataObject.TryGetProperty("amount_refunded", out var amountProp)
            ? amountProp.GetInt64() / 100m
            : 0m;

        var reason = dataObject.TryGetProperty("reason", out var reasonProp)
            ? reasonProp.GetString() ?? "webhook_refund"
            : "webhook_refund";

        var chargeId = dataObject.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        await sender.Send(
            new HandleStripeRefundCommand(paymentIntentId, amountRefunded, reason, chargeId),
            cancellationToken);
        logger.LogInformation(
            "Refund recorded via Stripe webhook (PI: {PaymentIntentId}, amount: {Amount})",
            paymentIntentId, amountRefunded);
    }
}
