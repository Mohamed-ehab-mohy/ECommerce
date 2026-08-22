using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Invoicing.Queries;

public sealed record InvoicePdfResult(byte[] Content, string FileName);

public sealed record DownloadInvoicePdfQuery(Guid InvoiceId)
    : IRequest<Result<InvoicePdfResult>>, IRequirePermission
{
    public string Permission => Permissions.FinanceInvoiceRead;
}
