using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Responses;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class AssignFulfillmentTaskCommandHandler(
    IFulfillmentTaskRepository tasks,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<AssignFulfillmentTaskCommand> validator) : IRequestHandler<AssignFulfillmentTaskCommand, Result<FulfillmentTaskResponse>>
{
    public async Task<Result<FulfillmentTaskResponse>> Handle(AssignFulfillmentTaskCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<FulfillmentTaskResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return FulfillmentErrors.TaskNotFound;
        }

        var assignment = task.Assign(request.AssigneeId, utcNow);
        if (assignment.IsFailure)
        {
            return assignment.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return FulfillmentTaskResponse.From(task);
    }
}

public sealed class StartPickingFulfillmentTaskCommandHandler(
    IFulfillmentTaskRepository tasks,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<StartPickingFulfillmentTaskCommand> validator) : IRequestHandler<StartPickingFulfillmentTaskCommand, Result<FulfillmentTaskResponse>>
{
    public async Task<Result<FulfillmentTaskResponse>> Handle(StartPickingFulfillmentTaskCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<FulfillmentTaskResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return FulfillmentErrors.TaskNotFound;
        }

        var result = task.StartPicking(utcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var order = await orders.GetByIdAsync(task.OrderId, cancellationToken);
        if (order is not null && order.Status == OrderStatus.AwaitingFulfillment)
        {
            var start = order.StartFulfillment("user", null, null, utcNow);
            if (start.IsFailure)
            {
                return start.Error;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return FulfillmentTaskResponse.From(task);
    }
}

public sealed class MarkFulfillmentTaskPackedCommandHandler(
    IFulfillmentTaskRepository tasks,
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<MarkFulfillmentTaskPackedCommand> validator) : IRequestHandler<MarkFulfillmentTaskPackedCommand, Result<FulfillmentTaskResponse>>
{
    public async Task<Result<FulfillmentTaskResponse>> Handle(MarkFulfillmentTaskPackedCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<FulfillmentTaskResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return FulfillmentErrors.TaskNotFound;
        }

        var result = task.MarkPacked(utcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var order = await orders.GetByIdAsync(task.OrderId, cancellationToken);
        if (order is not null && order.Status == OrderStatus.Picking)
        {
            var packed = order.MarkPacked("user", null, null, utcNow);
            if (packed.IsFailure)
            {
                return packed.Error;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return FulfillmentTaskResponse.From(task);
    }
}
