using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;

namespace ECommerce.UseCases.Cart.Queries;

public sealed record GetCartPriceChangesQuery(string OwnerKey) : IRequest<Result<CartPriceChangesResponse>>;
