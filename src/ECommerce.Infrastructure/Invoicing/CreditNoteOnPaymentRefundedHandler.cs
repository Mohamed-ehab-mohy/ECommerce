using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Services;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class CreditNoteOnPaymentRefundedHandler(
    InvoiceIssuanceService issuanceService,
    ILogger<CreditNoteOnPaymentRefundedHandler> logger) : IEventHandler<PaymentRefunded>
{
    public async Task HandleAsync(PaymentRefunded domainEvent, CancellationToken cancellationToken)
    {
        var result = await issuanceService.IssueForRefundAsync(domainEvent, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Could not issue credit note for refund {PaymentId} (order {OrderId}): {Error}",
                domainEvent.PaymentId,
                domainEvent.OrderId,
                result.Error.Description);
        }
    }
}
