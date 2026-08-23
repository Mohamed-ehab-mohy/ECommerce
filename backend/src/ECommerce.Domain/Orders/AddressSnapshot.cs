namespace ECommerce.Domain.Orders;

public sealed record AddressSnapshot(
    string FullName,
    string? Phone,
    string Street,
    string City,
    string? Region,
    string Country,
    string PostalCode);
