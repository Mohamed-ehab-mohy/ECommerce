using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Responses;

namespace ECommerce.UseCases.Invoicing.Queries;

public sealed record ListCreditNotesQuery(Guid InvoiceId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedCreditNotesResponse>>, IRequirePermission
{
    public string Permission => Permissions.FinanceInvoiceRead;
}

public sealed record PagedCreditNotesResponse(
    IReadOnlyList<CreditNoteResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
