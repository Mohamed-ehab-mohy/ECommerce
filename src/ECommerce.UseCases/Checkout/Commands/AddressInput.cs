namespace ECommerce.UseCases.Checkout.Commands;

public sealed record AddressInput(
    string FullName,
    string? Phone,
    string Street,
    string City,
    string? Region,
    string Country,
    string PostalCode);
