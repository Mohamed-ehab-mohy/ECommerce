namespace ECommerce.UseCases.Identity.Responses;

public sealed record AddressResponse(
    Guid Id,
    string? Label,
    string Street,
    string City,
    string? Region,
    string Country,
    string? PostalCode,
    DateTime CreatedAt);
