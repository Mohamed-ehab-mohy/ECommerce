using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Checkout.Responses;
using MediatR;

namespace ECommerce.UseCases.Checkout.Queries;

public sealed record GetCheckoutQuery(Guid CheckoutId) : IRequest<Result<CheckoutResponse>>;
