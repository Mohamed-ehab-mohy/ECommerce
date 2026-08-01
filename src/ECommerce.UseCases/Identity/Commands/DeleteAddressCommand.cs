using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record DeleteAddressCommand(Guid CustomerId, Guid AddressId) : IRequest<Result>;
