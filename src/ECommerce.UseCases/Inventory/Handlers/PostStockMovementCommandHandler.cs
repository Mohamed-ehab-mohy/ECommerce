using ECommerce.Domain.Audit;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Inventory;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Ports;

namespace ECommerce.UseCases.Inventory.Handlers;

public sealed class PostStockMovementCommandHandler(
    IStockRepository stock,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<PostStockMovementCommand> validator,
    IAuditLogWriter auditLogWriter) : IRequestHandler<PostStockMovementCommand, Result>
{
    public async Task<Result> Handle(PostStockMovementCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var sku = request.Sku.Trim().ToUpperInvariant();
        var type = Enum.Parse<StockMovementType>(request.Type, ignoreCase: true);

        if (type == StockMovementType.Adjustment && request.Quantity < 0 && request.ApprovedBy is null)
        {
            return StockErrors.ApprovalRequired;
        }

        var stockItem = await stock.GetBySkuAndWarehouseAsync(sku, request.WarehouseId, cancellationToken);
        var created = stockItem is null;
        stockItem ??= StockItem.Create(sku, request.WarehouseId, utcNow);

        var movement = StockMovement.Create(
            stockItem.Id,
            type,
            request.Quantity,
            request.Reason,
            request.Reference,
            request.Note,
            utcNow);

        var before = new { stockItem.OnHand, stockItem.Allocated };
        try
        {
            stockItem.Apply(movement, utcNow);
        }
        catch (StockBalanceException ex)
        {
            return Result.Failure(ex.Error);
        }

        if (created)
        {
            stock.Add(stockItem);
        }

        stock.AddMovement(movement);

        await auditLogWriter.WriteAsync(new AuditOperation(
            AuditActions.StockMovementPosted,
            "StockItem",
            stockItem.Id.ToString(),
            before,
            new { stockItem.OnHand, stockItem.Allocated }), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
