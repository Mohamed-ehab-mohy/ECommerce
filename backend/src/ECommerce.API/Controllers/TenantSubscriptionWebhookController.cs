using System.Text.Json;
using ECommerce.Infrastructure.Payments;
using ECommerce.UseCases.Tenants.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/webhooks/stripe/subscription")]
public sealed class TenantSubscriptionWebhookController(
    ISender sender,
    IOptions<StripeWebhookOptions> webhookOptions,
    ILogger<TenantSubscriptionWebhookController> logger) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader))
        {
            logger.LogWarning("Subscription webhook received without signature header");
            return Unauthorized();
        }

        if (!StripeSignatureVerifier.Verify(rawBody, signatureHeader.ToString(), webhookOptions.Value.WebhookSecret))
        {
            logger.LogWarning("Subscription webhook signature verification failed");
            return Unauthorized();
        }

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventType = root.GetProperty("type").GetString();

        if (eventType is "customer.subscription.updated" or "customer.subscription.deleted")
        {
            var sub = root.GetProperty("data").GetProperty("object");

            var stripeCustomerId = sub.GetProperty("customer").GetString();
            var stripeSubscriptionId = sub.GetProperty("id").GetString();
            var status = sub.GetProperty("status").GetString();

            if (string.IsNullOrWhiteSpace(stripeCustomerId) ||
                string.IsNullOrWhiteSpace(stripeSubscriptionId) ||
                string.IsNullOrWhiteSpace(status))
            {
                logger.LogWarning("Subscription webhook payload is missing required fields");
                return BadRequest();
            }
            var currentPeriodEndEpoch = sub.TryGetProperty("current_period_end", out var periodProp)
                ? periodProp.GetInt64()
                : 0L;

            var result = await sender.Send(
                new HandleSubscriptionUpdatedCommand(
                    stripeCustomerId,
                    stripeSubscriptionId,
                    status,
                    currentPeriodEndEpoch),
                cancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning("Subscription webhook processing failed: {Error}", result.Error?.Description);
                return BadRequest(result.Error?.Description);
            }
        }

        return Ok();
    }
}
