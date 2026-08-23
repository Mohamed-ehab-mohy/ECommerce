using ECommerce.UseCases.Common;
using ECommerce.UseCases.Coupons.Queries;
using ECommerce.UseCases.Coupons.Responses;
using ECommerce.UseCases.Promotions.Ports;

namespace ECommerce.UseCases.Coupons.Handlers;

public sealed class GetCouponsQueryHandler(ICouponRepository coupons)
    : IRequestHandler<GetCouponsQuery, Result<IReadOnlyList<CouponResponse>>>
{
    public async Task<Result<IReadOnlyList<CouponResponse>>> Handle(
        GetCouponsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await coupons.GetAllAsync(cancellationToken);

        return all.Select(CouponResponse.From).ToList();
    }
}
