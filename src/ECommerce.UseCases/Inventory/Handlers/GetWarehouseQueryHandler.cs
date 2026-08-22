using ECommerce.Domain.Inventory;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Inventory.Queries;
using ECommerce.UseCases.Inventory.Responses;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class GetWarehouseQueryHandler(
    IWarehouseRepository warehouses,
    IValidator<GetWarehouseQuery> validator) : IRequestHandler<GetWarehouseQuery, Result<WarehouseResponse>>
{
    public async Task<Result<WarehouseResponse>> Handle(GetWarehouseQuery request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<WarehouseResponse>();
        }

        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);

        return warehouse is null
            ? Result<WarehouseResponse>.Failure(WarehouseErrors.WarehouseNotFound)
            : Result<WarehouseResponse>.Success(ToResponse(warehouse));
    }

    internal static WarehouseResponse ToResponse(Warehouse warehouse) =>
        new(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.Timezone, warehouse.Status);
}
