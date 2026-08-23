using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Responses;

namespace ECommerce.UseCases.Identity.Queries;

public sealed record SearchCustomersQuery(
    string? Email = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedCustomersResponse>>, IRequirePermission
{
    public string Permission => Permissions.CustomersRead;
}
