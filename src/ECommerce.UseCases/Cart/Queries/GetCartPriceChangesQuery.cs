using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;
using MediatR;

namespace ECommerce.UseCases.Cart.Queries;

public sealed record GetCartPriceChangesQuery(string OwnerKey) : IRequest<Result<CartPriceChangesResponse>>;
