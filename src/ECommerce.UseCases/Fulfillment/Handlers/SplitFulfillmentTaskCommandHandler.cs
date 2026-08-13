using ECommerce.Domain.Fulfillment;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Ports;
using ECommerce.UseCases.Fulfillment.Responses;
using ECommerce.UseCases.Inventory.Ports;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class SplitFulfillmentTaskCommandHandler(
    IFulfillmentTaskRepository tasks,
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<SplitFulfillmentTaskCommand> validator) : IRequestHandler<SplitFulfillmentTaskCommand, Result<FulfillmentTaskResponse>>
{
    public async Task<Result<FulfillmentTaskResponse>> Handle(SplitFulfillmentTaskCommand request, CancellationToken cancellationToken)
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

        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return FulfillmentErrors.WarehouseNotFound;
        }

        var split = task.Split(
            warehouse.Id,
            request.ItemIds,
            request.Priority,
            request.Zone,
            utcNow);
        if (split.IsFailure)
        {
            return split.Error;
        }

        tasks.Add(split.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return FulfillmentTaskResponse.From(split.Value);
    }
}
