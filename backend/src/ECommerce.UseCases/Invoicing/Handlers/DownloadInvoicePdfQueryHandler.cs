using ECommerce.Domain.Invoicing;
using ECommerce.UseCases.Invoicing.Ports;
using ECommerce.UseCases.Invoicing.Queries;

namespace ECommerce.UseCases.Invoicing.Handlers;

public sealed class DownloadInvoicePdfQueryHandler(
    IInvoiceRepository invoices,
    IInvoiceDocumentStore documentStore) : IRequestHandler<DownloadInvoicePdfQuery, Result<InvoicePdfResult>>
{
    public async Task<Result<InvoicePdfResult>> Handle(DownloadInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        if (string.IsNullOrWhiteSpace(invoice.PdfUrl))
        {
            return InvoiceErrors.InvoiceNotFound;
        }

        var content = await documentStore.GetAsync(invoice.PdfUrl, cancellationToken);

        return content is null
            ? InvoiceErrors.InvoiceNotFound
            : new InvoicePdfResult(content, $"{invoice.InvoiceNumber}.pdf");
    }
}
