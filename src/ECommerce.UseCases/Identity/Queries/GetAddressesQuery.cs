using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Responses;
using MediatR;

namespace ECommerce.UseCases.Identity.Queries;

public sealed record GetAddressesQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<AddressResponse>>>;
