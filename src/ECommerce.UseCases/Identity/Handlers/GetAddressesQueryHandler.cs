using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Identity.Queries;
using ECommerce.UseCases.Identity.Responses;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class GetAddressesQueryHandler(IAddressRepository addresses) : IRequestHandler<GetAddressesQuery, Result<IReadOnlyList<AddressResponse>>>
{
    public async Task<Result<IReadOnlyList<AddressResponse>>> Handle(GetAddressesQuery request, CancellationToken cancellationToken)
    {
        var customerAddresses = await addresses.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        var items = customerAddresses
            .OrderBy(address => address.CreatedAt)
            .Select(address => new AddressResponse(
                address.Id,
                address.Label,
                address.Street,
                address.City,
                address.Region,
                address.Country,
                address.PostalCode,
                address.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<AddressResponse>>.Success(items);
    }
}
