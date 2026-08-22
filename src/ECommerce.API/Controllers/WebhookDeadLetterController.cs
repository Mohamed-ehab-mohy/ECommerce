using ECommerce.UseCases.Common;
using ECommerce.UseCases.Integrations.Ports;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1/webhooks/dead-letter")]
public sealed class WebhookDeadLetterController(
    IWebhookDeadLetterRepository deadLetterRepository,
    IWebhookDeliveryJobScheduler scheduler,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? eventType = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await deadLetterRepository.ListAsync(limit, offset, eventType, cancellationToken);
        var total = await deadLetterRepository.CountAsync(eventType, cancellationToken);

        return Ok(new
        {
            total,
            offset,
            limit,
            entries
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await deadLetterRepository.GetByIdAsync(id, cancellationToken);

        return entry is null
            ? NotFound()
            : Ok(entry);
    }

    [HttpPost("{id:guid}/replay")]
    public async Task<IActionResult> Replay(Guid id, CancellationToken cancellationToken)
    {
        var entry = await deadLetterRepository.GetByIdAsync(id, cancellationToken);

        return entry is null
            ? NotFound()
            : entry.IsReplayed
                ? Conflict(new { message = "This delivery has already been replayed." })
                : await ReplayDeliveryAsync(entry.Id, entry.DeliveryId, cancellationToken);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var total = await deadLetterRepository.CountAsync(null, cancellationToken);
        var orderPlaced = await deadLetterRepository.CountAsync(WebhookEventTypesCatalog.OrderPlaced, cancellationToken);
        var orderPaid = await deadLetterRepository.CountAsync(WebhookEventTypesCatalog.OrderPaid, cancellationToken);
        var orderShipped = await deadLetterRepository.CountAsync(WebhookEventTypesCatalog.OrderShipped, cancellationToken);
        var refundCompleted = await deadLetterRepository.CountAsync(WebhookEventTypesCatalog.RefundCompleted, cancellationToken);

        return Ok(new
        {
            total,
            byEventType = new
            {
                order_placed = orderPlaced,
                order_paid = orderPaid,
                order_shipped = orderShipped,
                refund_completed = refundCompleted
            }
        });
    }

    private async Task<IActionResult> ReplayDeliveryAsync(Guid entryId, Guid deliveryId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var replayed = await deadLetterRepository.MarkDeliveryReplayedAsync(entryId, now, cancellationToken);
        if (!replayed)
        {
            return NotFound(new { message = "Delivery not found." });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        scheduler.Enqueue(deliveryId);

        return Ok(new { message = "Delivery replayed.", deliveryId });
    }
}
