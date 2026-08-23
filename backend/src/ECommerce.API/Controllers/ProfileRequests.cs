namespace ECommerce.API.Controllers;

public sealed record UpdateProfileRequest(string? DisplayName, string? Phone, string? Locale, string? Currency);

public sealed record AddAddressRequest(
    string? Label,
    string Street,
    string City,
    string? Region,
    string Country,
    string? PostalCode);
