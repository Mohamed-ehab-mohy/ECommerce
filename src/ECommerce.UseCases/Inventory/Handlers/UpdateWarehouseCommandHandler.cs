using ECommerce.Domain.Audit;
using ECommerce.Domain.Inventory;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class UpdateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<UpdateWarehouseCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<UpdateWarehouseCommand, Result>
{
    public async Task<Result> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
        {
            return Result.Failure(WarehouseErrors.WarehouseNotFound);
        }

        var before = new { warehouse.Name, warehouse.Address, warehouse.Timezone, warehouse.Status };

        warehouse.UpdateDetails(
            request.Name?.Trim(),
            request.Address?.Trim(),
            request.Timezone?.Trim(),
            ParseStatus(request.Status),
            timeProvider.GetUtcNow().UtcDateTime);

        var after = new { warehouse.Name, warehouse.Address, warehouse.Timezone, warehouse.Status };

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.WarehouseUpdated,
            "Warehouse",
            warehouse.Id.ToString(),
            before,
            after), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static WarehouseStatus? ParseStatus(string? status) =>
        status is null
            ? null
            : Enum.TryParse<WarehouseStatus>(status, ignoreCase: true, out var parsed)
                ? parsed
                : null;
}
