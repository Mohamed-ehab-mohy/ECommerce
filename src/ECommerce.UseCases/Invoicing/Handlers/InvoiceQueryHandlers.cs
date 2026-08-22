using ECommerce.Domain.Invoicing;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Ports;
using ECommerce.UseCases.Invoicing.Queries;
using ECommerce.UseCases.Invoicing.Responses;

namespace ECommerce.UseCases.Invoicing.Handlers;

public sealed class GetInvoiceQueryHandler(IInvoiceRepository invoices)
    : IRequestHandler<GetInvoiceQuery, Result<InvoiceResponse>>
{
    public async Task<Result<InvoiceResponse>> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(request.InvoiceId, cancellationToken);
        return invoice is null
            ? InvoiceErrors.InvoiceNotFound
            : InvoiceResponse.From(invoice);
    }
}

public sealed class ListInvoicesQueryHandler(IInvoiceRepository invoices)
    : IRequestHandler<ListInvoicesQuery, Result<PagedInvoicesResponse>>
{
    public async Task<Result<PagedInvoicesResponse>> Handle(ListInvoicesQuery request, CancellationToken cancellationToken)
    {
        InvoiceStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<InvoiceStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            status = parsed;
        }

        var page = await invoices.ListAsync(status, request.Page, request.PageSize, cancellationToken);

        return new PagedInvoicesResponse(
            page.Items.Select(InvoiceResponse.From).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }
}

public sealed class ListCreditNotesQueryHandler(ICreditNoteRepository creditNotes)
    : IRequestHandler<ListCreditNotesQuery, Result<PagedCreditNotesResponse>>
{
    public async Task<Result<PagedCreditNotesResponse>> Handle(ListCreditNotesQuery request, CancellationToken cancellationToken)
    {
        var page = await creditNotes.ListByInvoiceAsync(request.InvoiceId, request.Page, request.PageSize, cancellationToken);

        return new PagedCreditNotesResponse(
            page.Items.Select(CreditNoteResponse.From).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }
}
