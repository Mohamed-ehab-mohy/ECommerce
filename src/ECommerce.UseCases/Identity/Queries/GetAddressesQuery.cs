using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Responses;

namespace ECommerce.UseCases.Identity.Queries;

public sealed record GetAddressesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<AddressResponse>>>;
