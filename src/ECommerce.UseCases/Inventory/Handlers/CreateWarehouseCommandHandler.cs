using ECommerce.Domain.Audit;
using ECommerce.Domain.Inventory;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class CreateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateWarehouseCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<CreateWarehouseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<Guid>();
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await warehouses.GetByCodeAsync(code, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(WarehouseErrors.CodeAlreadyExists);
        }

        var warehouse = Warehouse.Create(
            code,
            request.Name.Trim(),
            request.Address.Trim(),
            request.Timezone.Trim(),
            ParseStatus(request.Status),
            timeProvider.GetUtcNow().UtcDateTime);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.WarehouseCreated,
            "Warehouse",
            warehouse.Id.ToString(),
            After: new { warehouse.Code, warehouse.Name, warehouse.Address, warehouse.Timezone, warehouse.Status }), cancellationToken);

        warehouses.Add(warehouse);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(warehouse.Id);
    }

    private static WarehouseStatus ParseStatus(string? status) =>
        Enum.TryParse<WarehouseStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : WarehouseStatus.Active;
}
