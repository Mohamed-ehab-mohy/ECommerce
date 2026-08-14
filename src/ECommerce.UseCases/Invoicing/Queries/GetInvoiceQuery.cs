using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Responses;
using MediatR;

namespace ECommerce.UseCases.Invoicing.Queries;

public sealed record GetInvoiceQuery(Guid InvoiceId)
    : IRequest<Result<InvoiceResponse>>, IRequirePermission
{
    public string Permission => Permissions.FinanceInvoiceRead;
}
