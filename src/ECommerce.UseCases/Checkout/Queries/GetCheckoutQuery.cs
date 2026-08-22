using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Responses;

namespace ECommerce.UseCases.Checkout.Queries;

public sealed record GetCheckoutQuery(Guid CheckoutId) : IRequest<Result<CheckoutResponse>>;
