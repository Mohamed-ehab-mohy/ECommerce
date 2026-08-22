using ECommerce.Domain.Audit;
using ECommerce.Domain.Inventory;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class DeactivateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork,
    IAuditLogWriter auditLogWriter) : IRequestHandler<DeactivateWarehouseCommand, Result>
{
    public async Task<Result> Handle(DeactivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return Result.Failure(WarehouseErrors.WarehouseNotFound);
        }

        var before = new { warehouse.Status };

        warehouse.Deactivate();

        var after = new { warehouse.Status };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.WarehouseDeactivated,
            "Warehouse",
            warehouse.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
