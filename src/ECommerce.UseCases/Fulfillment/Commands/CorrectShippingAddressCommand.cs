using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using MediatR;

namespace ECommerce.UseCases.Fulfillment.Commands;

public sealed record CorrectShippingAddressCommand(
    Guid OrderId,
    string FullName,
    string? Phone,
    string Street,
    string City,
    string? Region,
    string Country,
    string PostalCode) : IRequest<Result>, IRequirePermission
{
    public string Permission => Permissions.FulfillmentWrite;
}
