using ECommerce.Domain.Inventory;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Inventory.Commands;
using ECommerce.UseCases.Inventory.Handlers;

namespace ECommerce.UnitTests;

public sealed class StockCommandHandlerTests
{
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly FakeStockRepository _stock = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly FakeAuditEntryRepository _auditEntries = new();
    private readonly FakeAuditContextProvider _auditContext = new();

    private PostStockMovementCommandHandler Handler =>
        new(_stock, _unitOfWork, _timeProvider, new PostStockMovementCommandValidator(),
            new AuditLogWriter(_auditEntries, _auditContext));

    [Fact]
    public async Task PostMovement_Creates_StockItem_And_Movement_When_None_Exists()
    {
        var result = await Handler.Handle(
            new PostStockMovementCommand("sku-1", WarehouseId, "Receipt", 10, "PO-100", "ref-1", "Initial stock"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _unitOfWork.SaveCount);

        var item = Assert.Single(_stock.Items);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(10, item.OnHand);

        var movement = Assert.Single(_stock.Movements);
        Assert.Equal(item.Id, movement.StockItemId);
        Assert.Equal(StockMovementType.Receipt, movement.Type);
        Assert.Equal(10, movement.OnHandDelta);

        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task PostMovement_Applies_To_Existing_StockItem()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, DateTime.UtcNow);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Receipt, 10, "RECV", null, null, DateTime.UtcNow), DateTime.UtcNow);
        _stock.Items.Add(item);

        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Issue", 3, "SALE-1", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, item.OnHand);
        Assert.Single(_stock.Movements);
        Assert.Single(_auditEntries.Entries);
    }

    [Fact]
    public async Task PostMovement_Rejects_Movement_That_Would_Go_Negative()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, DateTime.UtcNow);
        _stock.Items.Add(item);

        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Issue", 5, "SALE-1", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(StockErrors.InsufficientOnHand, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Empty(_stock.Movements);
        Assert.Empty(_auditEntries.Entries);
    }

    [Fact]
    public async Task PostMovement_With_Invalid_Type_Returns_Validation_Failure()
    {
        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Transfer", 5, "TRF", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Empty(_stock.Items);
        Assert.Empty(_stock.Movements);
    }

    [Fact]
    public async Task PostMovement_With_Zero_Quantity_Returns_Validation_Failure()
    {
        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Receipt", 0, "PO-1", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task PostMovement_With_Negative_Quantity_On_Adjustment_Is_Allowed()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, DateTime.UtcNow);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Receipt, 10, "RECV", null, null, DateTime.UtcNow), DateTime.UtcNow);
        _stock.Items.Add(item);

        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Adjustment", -4, "COUNT-1", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, item.OnHand);
        Assert.Single(_stock.Movements);
    }

    [Fact]
    public async Task PostMovement_With_Negative_Quantity_On_NonAdjustment_Returns_Validation_Failure()
    {
        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Receipt", -5, "PO-1", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task PostMovement_With_Blank_Reason_Returns_Validation_Failure()
    {
        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Receipt", 5, " ", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task PostMovement_Tracks_Allocation_Then_Fulfillment_End_To_End()
    {
        await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Receipt", 10, "PO-1", null, null),
            CancellationToken.None);

        await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Allocate", 4, "ORD-1", null, null),
            CancellationToken.None);

        var item = Assert.Single(_stock.Items);
        Assert.Equal(4, item.Allocated);
        Assert.Equal(6, item.Available);

        var result = await Handler.Handle(
            new PostStockMovementCommand("SKU-1", WarehouseId, "Fulfill", 4, "ORD-1", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, item.OnHand);
        Assert.Equal(0, item.Allocated);
        Assert.Equal(3, _stock.Movements.Count);
    }
}
