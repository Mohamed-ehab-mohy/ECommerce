using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Responses;

namespace ECommerce.UseCases.Invoicing.Queries;

public sealed record GetInvoiceQuery(Guid InvoiceId)
    : IRequest<Result<InvoiceResponse>>, IRequirePermission
{
    public string Permission => Permissions.FinanceInvoiceRead;
}
