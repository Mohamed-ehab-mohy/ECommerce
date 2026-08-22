using ECommerce.Domain.Audit;
using ECommerce.Domain.Events;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Inventory;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class TransferStockCommandHandler(
    IStockRepository stock,
    IWarehouseRepository warehouses,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<TransferStockCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<TransferStockCommand, Result>
{
    public async Task<Result> Handle(TransferStockCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var sku = request.Sku.Trim().ToUpperInvariant();

        var fromWarehouse = await warehouses.GetByIdAsync(request.FromWarehouseId, cancellationToken);
        var toWarehouse = await warehouses.GetByIdAsync(request.ToWarehouseId, cancellationToken);

        if (fromWarehouse is null || toWarehouse is null)
        {
            return WarehouseErrors.WarehouseNotFound;
        }

        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            return StockErrors.SameWarehouseTransfer;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var items = await stock.LockForTransferAsync(
            sku,
            request.FromWarehouseId,
            request.ToWarehouseId,
            cancellationToken);

        var source = items.FirstOrDefault(item => item.WarehouseId == request.FromWarehouseId);
        var target = items.FirstOrDefault(item => item.WarehouseId == request.ToWarehouseId);

        if (source is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StockErrors.StockItemNotFound;
        }

        target ??= StockItem.Create(sku, request.ToWarehouseId, utcNow);

        var issue = StockMovement.Create(
            source.Id,
            StockMovementType.Issue,
            request.Quantity,
            "TRANSFER",
            request.ToWarehouseId.ToString(),
            request.Note,
            utcNow);

        var receipt = StockMovement.Create(
            target.Id,
            StockMovementType.Receipt,
            request.Quantity,
            "TRANSFER",
            request.FromWarehouseId.ToString(),
            request.Note,
            utcNow);

        var before = new
        {
            SourceOnHand = source.OnHand,
            SourceAllocated = source.Allocated,
            TargetOnHand = target.OnHand,
            TargetAllocated = target.Allocated
        };

        try
        {
            source.Apply(issue, utcNow);
            target.Apply(receipt, utcNow);
        }
        catch (StockBalanceException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return exception.Error;
        }

        source.AddDomainEvent(new StockTransferred(
            source.Id,
            target.Id,
            sku,
            request.FromWarehouseId,
            request.ToWarehouseId,
            request.Quantity));

        if (items.All(item => item.Id != target.Id))
        {
            stock.Add(target);
        }

        stock.AddMovement(issue);
        stock.AddMovement(receipt);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.StockMovementPosted,
            "StockTransfer",
            sku,
            before,
            new
            {
                SourceOnHand = source.OnHand,
                SourceAllocated = source.Allocated,
                TargetOnHand = target.OnHand,
                TargetAllocated = target.Allocated
            }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
