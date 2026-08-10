using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Responses;
using MediatR;

namespace ECommerce.UseCases.Orders.Queries;

public sealed record SupportOrderLookupQuery(
    string? OrderNumber = null,
    string? Email = null,
    Guid? CustomerId = null) : IRequest<Result<SupportOrderLookupResponse>>, IRequirePermission
{
    public string Permission => Permissions.OrdersSupportRead;
}
