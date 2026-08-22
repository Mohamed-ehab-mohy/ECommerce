using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Cart.Responses;

namespace ECommerce.UseCases.Cart.Commands;

public sealed record RemoveCartCouponCommand(
    string OwnerKey) : IRequest<Result<CartResponse>>;
