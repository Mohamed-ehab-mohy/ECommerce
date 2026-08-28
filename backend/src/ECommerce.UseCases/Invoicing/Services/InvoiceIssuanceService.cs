using ECommerce.Domain.Events;
using ECommerce.Domain.Invoicing;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Ports;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Invoicing.Services;

/// <summary>
/// Creates invoices when payments are captured and credit notes when refunds complete
///.
/// </summary>
public sealed class InvoiceIssuanceService(
    IOrderRepository orders,
    IInvoiceRepository invoices,
    ICreditNoteRepository creditNotes,
    IInvoiceNumberGenerator invoiceNumberGenerator,
    ICreditNoteNumberGenerator creditNoteNumberGenerator,
    IInvoicePdfJobScheduler pdfJobScheduler,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<Invoice>> IssueForPaymentCapturedAsync(
        PaymentCaptured paymentCaptured,
        CancellationToken cancellationToken)
    {
        if (paymentCaptured.OrderId is not { } orderId)
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        var existing = await invoices.GetByOrderIdAsync(order.Id, cancellationToken);
        if (existing is not null)
        {
            pdfJobScheduler.Enqueue(existing.Id);
            return existing;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var invoiceNumber = await invoiceNumberGenerator.GenerateAsync(utcNow, cancellationToken);

        var lines = order.Items
            .Select(item => InvoiceLine.Create(
                Guid.Empty,
                item.Sku,
                $"{item.Name} ({item.Sku})",
                item.Quantity,
                item.UnitPrice,
                order.TaxRate,
                Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero)))
            .ToList();

        var invoice = Invoice.Create(
            invoiceNumber.Value,
            order.Id,
            order.CustomerId,
            order.Currency,
            lines,
            order.TaxTotal,
            order.TaxRate,
            order.GrandTotal,
            utcNow);

        invoices.Add(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        pdfJobScheduler.Enqueue(invoice.Id);

        return invoice;
    }

    public async Task<Result<CreditNote>> IssueForRefundAsync(
        PaymentRefunded paymentRefunded,
        CancellationToken cancellationToken)
    {
        if (paymentRefunded.OrderId is not { } orderId)
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        var invoice = await invoices.GetByOrderIdAsync(orderId, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        var existingCreditNote = await creditNotes.GetByRefundIdAsync(paymentRefunded.PaymentId, cancellationToken);
        if (existingCreditNote is not null)
        {
            return existingCreditNote;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var creditNoteNumber = await creditNoteNumberGenerator.GenerateAsync(utcNow, cancellationToken);

        var creditNote = CreditNote.Create(
            creditNoteNumber.Value,
            invoice.Id,
            paymentRefunded.PaymentId,
            paymentRefunded.Amount,
            "Refund completed",
            invoice.Currency,
            utcNow);

        var apply = invoice.ApplyCreditNote(paymentRefunded.Amount, utcNow);
        if (apply.IsFailure)
        {
            return apply.Error;
        }

        creditNotes.Add(creditNote);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return creditNote;
    }
}
