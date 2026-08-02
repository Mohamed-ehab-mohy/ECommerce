using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Responses;
using MediatR;

namespace ECommerce.UseCases.Identity.Queries;

public sealed record GetCustomerQuery(Guid CustomerId) : IRequest<Result<CustomerLookupResponse>>, IRequirePermission
{
    public string Permission => Permissions.CustomersRead;
}
