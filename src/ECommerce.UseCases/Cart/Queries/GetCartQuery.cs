using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;
using MediatR;

namespace ECommerce.UseCases.Cart.Queries;

public sealed record GetCartQuery(
    string OwnerKey,
    string Currency) : IRequest<Result<CartResponse>>;
