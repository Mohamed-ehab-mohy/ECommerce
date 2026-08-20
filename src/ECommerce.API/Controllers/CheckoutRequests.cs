namespace ECommerce.API.Controllers;

public sealed record InitiateCheckoutRequest(
    Guid CartId,
    string CustomerEmail,
    string Currency,
    AddressRequest ShippingAddress,
    AddressRequest? BillingAddress,
    string ShippingMethodId,
    PaymentMethodRequest PaymentMethod);

public sealed record GuestInitiateCheckoutRequest(
    Guid CartId,
    string CustomerEmail,
    string Currency,
    AddressRequest ShippingAddress,
    AddressRequest? BillingAddress,
    string ShippingMethodId,
    PaymentMethodRequest PaymentMethod);

public sealed record AddressRequest(
    string FullName,
    string? Phone,
    string Street,
    string City,
    string? Region,
    string Country,
    string PostalCode);

public sealed record PaymentMethodRequest(string ProviderKey, string MethodType);
