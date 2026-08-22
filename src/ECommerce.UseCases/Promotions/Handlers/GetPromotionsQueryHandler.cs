using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Promotions.Ports;
using ECommerce.UseCases.Promotions.Queries;
using ECommerce.UseCases.Promotions.Responses;

namespace ECommerce.UseCases.Promotions.Handlers;

public sealed class GetPromotionsQueryHandler(IPromotionRepository promotions)
    : IRequestHandler<GetPromotionsQuery, Result<IReadOnlyList<PromotionResponse>>>
{
    public async Task<Result<IReadOnlyList<PromotionResponse>>> Handle(
        GetPromotionsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await promotions.GetAllAsync(cancellationToken);

        return all.Select(PromotionResponse.From).ToList();
    }
}
