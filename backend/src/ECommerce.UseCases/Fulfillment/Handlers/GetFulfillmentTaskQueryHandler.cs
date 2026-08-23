using ECommerce.Domain.Fulfillment;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Queries;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class GetFulfillmentTaskQueryHandler(
    IFulfillmentTaskRepository tasks) : IRequestHandler<GetFulfillmentTaskQuery, Result<FulfillmentTaskResponse>>
{
    public async Task<Result<FulfillmentTaskResponse>> Handle(GetFulfillmentTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken);

        return task is null
            ? Result<FulfillmentTaskResponse>.Failure(FulfillmentErrors.TaskNotFound)
            : Result<FulfillmentTaskResponse>.Success(FulfillmentTaskResponse.From(task));
    }
}
