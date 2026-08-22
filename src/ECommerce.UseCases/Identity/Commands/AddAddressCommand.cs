using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record AddAddressCommand(
    Guid CustomerId,
    string? Label,
    string Street,
    string City,
    string? Region,
    string Country,
    string? PostalCode) : IRequest<Result<Guid>>;
