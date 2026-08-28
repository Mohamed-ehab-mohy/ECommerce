using ECommerce.Domain.Invoicing;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Ports;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Invoicing.Services;

/// <summary>
/// Renders and stores an invoice PDF. Idempotent: skips invoices that already have a
/// stored document.
/// </summary>
public sealed class InvoicePdfGenerationService(
    IInvoiceRepository invoices,
    IOrderRepository orders,
    IInvoicePdfRenderer renderer,
    IInvoiceDocumentStore documentStore,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> GenerateAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        if (!string.IsNullOrWhiteSpace(invoice.PdfUrl))
        {
            return Result.Success();
        }

        var order = await orders.GetByIdAsync(invoice.OrderId, cancellationToken);
        if (order is null)
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        var document = BuildDocument(invoice, order);

        var bytes = renderer.Render(document);

        var key = $"invoices/{invoice.InvoiceNumber}.pdf";
        var storedUrl = await documentStore.PutAsync(key, bytes, cancellationToken);

        invoice.AttachPdf(storedUrl, DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static InvoiceDocument BuildDocument(Invoice invoice, Order order)
    {
        var address = order.BillingAddress;
        var billingAddress = string.Join(
            ", ",
            new[]
            {
                address.FullName,
                address.Street,
                address.City,
                address.Region,
                address.Country,
                address.PostalCode
            }.Where(part => !string.IsNullOrWhiteSpace(part)));

        return new InvoiceDocument(
            invoice.InvoiceNumber,
            order.OrderNumber,
            invoice.IssuedAt,
            invoice.Currency,
            order.CustomerEmail.Split('@')[0],
            order.CustomerEmail,
            billingAddress,
            invoice.Lines
                .Select(line => new InvoiceDocumentLine(
                    line.Sku,
                    line.Description,
                    line.Quantity,
                    line.UnitAmount,
                    line.Amount))
                .ToList(),
            order.Subtotal,
            order.ItemDiscount + order.CartDiscount,
            order.ShippingTotal,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.Total);
    }
}
