using ECommerce.Domain.Invoicing;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Responses;

namespace ECommerce.UseCases.Invoicing.Queries;

public sealed record ListInvoicesQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedInvoicesResponse>>, IRequirePermission
{
    public string Permission => Permissions.FinanceInvoiceRead;
}

public sealed record PagedInvoicesResponse(
    IReadOnlyList<InvoiceResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
