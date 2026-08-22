using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;

namespace ECommerce.UseCases.Cart.Queries;

public sealed record GetCartQuery(
    string OwnerKey,
    string Currency) : IRequest<Result<CartResponse>>;
