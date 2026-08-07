using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Responses;
using MediatR;

namespace ECommerce.UseCases.Checkout.Commands;

public sealed record InitiateCheckoutCommand(
    Guid CartId,
    Guid? CustomerId,
    string CustomerEmail,
    string Currency,
    AddressInput ShippingAddress,
    AddressInput? BillingAddress,
    string ShippingMethodId,
    string ProviderKey,
    string MethodType,
    string Country) : IRequest<Result<CheckoutResponse>>;
