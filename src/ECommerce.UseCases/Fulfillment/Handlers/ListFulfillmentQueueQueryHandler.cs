using ECommerce.Domain.Fulfillment;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Queries;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class ListFulfillmentQueueQueryHandler(
    IFulfillmentTaskRepository tasks) : IRequestHandler<ListFulfillmentQueueQuery, Result<PagedFulfillmentTasksResponse>>
{
    public async Task<Result<PagedFulfillmentTasksResponse>> Handle(ListFulfillmentQueueQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        FulfillmentTaskStatus? status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : Enum.TryParse(request.Status, ignoreCase: true, out FulfillmentTaskStatus parsed)
                ? parsed
                : null;

        var items = await tasks.ListAsync(request.WarehouseId, status, page, pageSize, cancellationToken);
        var total = await tasks.CountAsync(request.WarehouseId, status, cancellationToken);

        return new PagedFulfillmentTasksResponse(
            items.Select(FulfillmentTaskResponse.From).ToList(),
            page,
            pageSize,
            total);
    }
}
