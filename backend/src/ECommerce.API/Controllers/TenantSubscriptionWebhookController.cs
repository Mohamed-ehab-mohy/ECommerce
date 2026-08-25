using ECommerce.UseCases.Tenants.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/webhooks/stripe")]
[AllowAnonymous]
public sealed class TenantSubscriptionWebhookController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        // In a real application, we read the body, verify Stripe signature using StripeConfiguration.WebhookSecret,
        // and parse the StripeEvent.
        // Here, we provide the scaffold for processing subscription updates.
        
        var mockStripeEvent = new { Type = "customer.subscription.updated" }; // Simulated parsed event

        if (mockStripeEvent.Type is "customer.subscription.updated" or "customer.subscription.deleted")
        {
            // Map Stripe payload to command
            var command = new HandleSubscriptionUpdatedCommand(
                StripeCustomerId: "cus_mock", 
                StripeSubscriptionId: "sub_mock",
                Status: "active",
                CurrentPeriodEndEpoch: 1672531199
            );

            var result = await sender.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                // Log failure, depending on error either return 400 or 500 to tell Stripe to retry
                return BadRequest();
            }
        }

        return Ok();
    }
}