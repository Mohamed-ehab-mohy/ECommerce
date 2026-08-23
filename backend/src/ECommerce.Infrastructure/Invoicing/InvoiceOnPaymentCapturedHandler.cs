using ECommerce.Domain.Events;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Services;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class InvoiceOnPaymentCapturedHandler(
    InvoiceIssuanceService issuanceService,
    ILogger<InvoiceOnPaymentCapturedHandler> logger) : IEventHandler<PaymentCaptured>
{
    public async Task HandleAsync(PaymentCaptured domainEvent, CancellationToken cancellationToken)
    {
        var result = await issuanceService.IssueForPaymentCapturedAsync(domainEvent, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning(
                "Could not issue invoice for payment {PaymentId} (order {OrderId}): {Error}",
                domainEvent.PaymentId,
                domainEvent.OrderId,
                result.Error.Description);
        }
    }
}
