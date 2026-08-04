using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;
using MediatR;

namespace ECommerce.UseCases.Cart.Commands;

public sealed record AddCartItemCommand(
    string OwnerKey,
    string Currency,
    Guid ProductId,
    int Quantity) : IRequest<Result<CartResponse>>;
