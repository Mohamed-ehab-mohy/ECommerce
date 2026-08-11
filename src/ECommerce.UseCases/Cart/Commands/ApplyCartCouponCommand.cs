using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;
using MediatR;

namespace ECommerce.UseCases.Cart.Commands;

public sealed record ApplyCartCouponCommand(
    string OwnerKey,
    string Code) : IRequest<Result<CartResponse>>;
