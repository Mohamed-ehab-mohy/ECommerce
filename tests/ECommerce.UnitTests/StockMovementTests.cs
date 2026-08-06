using ECommerce.Domain.Exceptions;
using ECommerce.Domain.Inventory;

namespace ECommerce.UnitTests;

public sealed class StockMovementTests
{
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static readonly DateTime Now = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(StockMovementType.Receipt, 10, 10, 0)]
    [InlineData(StockMovementType.Issue, 5, -5, 0)]
    [InlineData(StockMovementType.Adjustment, 7, 7, 0)]
    [InlineData(StockMovementType.Adjustment, -3, -3, 0)]
    [InlineData(StockMovementType.Allocate, 4, 0, 4)]
    [InlineData(StockMovementType.Release, 2, 0, -2)]
    [InlineData(StockMovementType.Fulfill, 3, -3, -3)]
    public void Create_Computes_Deltas(
        StockMovementType type,
        int quantity,
        int expectedOnHandDelta,
        int expectedAllocatedDelta)
    {
        var movement = StockMovement.Create(Guid.NewGuid(), type, quantity, "RECV", null, null, Now);

        Assert.Equal(expectedOnHandDelta, movement.OnHandDelta);
        Assert.Equal(expectedAllocatedDelta, movement.AllocatedDelta);
        Assert.Equal(quantity, movement.Quantity);
        Assert.Equal("RECV", movement.Reason);
        Assert.Equal(Now, movement.CreatedAt);
    }

    [Fact]
    public void Create_With_Zero_Quantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StockMovement.Create(Guid.NewGuid(), StockMovementType.Receipt, 0, "RECV", null, null, Now));
    }

    [Fact]
    public void Create_With_Negative_NonAdjustment_Quantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StockMovement.Create(Guid.NewGuid(), StockMovementType.Issue, -5, "ISSUE", null, null, Now));
    }

    [Fact]
    public void Create_With_Blank_Reason_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => StockMovement.Create(Guid.NewGuid(), StockMovementType.Receipt, 5, "  ", null, null, Now));
    }

    [Fact]
    public void Create_Normalizes_Optional_Fields()
    {
        var movement = StockMovement.Create(
            Guid.NewGuid(), StockMovementType.Receipt, 5, "RECV", "PO-100 ", "  note  ", Now);

        Assert.Equal("PO-100", movement.Reference);
        Assert.Equal("note", movement.Note);
    }

    [Fact]
    public void Apply_Receipt_Then_Fulfill_Tracks_Balances()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, Now);

        item.Apply(StockMovement.Create(item.Id, StockMovementType.Receipt, 10, "RECV", null, null, Now), Now);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Allocate, 4, "ORD-1", null, null, Now), Now);

        Assert.Equal(10, item.OnHand);
        Assert.Equal(4, item.Allocated);
        Assert.Equal(6, item.Available);

        item.Apply(StockMovement.Create(item.Id, StockMovementType.Fulfill, 4, "ORD-1", null, null, Now), Now);

        Assert.Equal(6, item.OnHand);
        Assert.Equal(0, item.Allocated);
        Assert.Equal(6, item.Available);
    }

    [Fact]
    public void Apply_Issue_Below_Zero_Throws_Insufficient_OnHand()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, Now);

        var exception = Assert.Throws<StockBalanceException>(
            () => item.Apply(StockMovement.Create(item.Id, StockMovementType.Issue, 5, "ISSUE", null, null, Now), Now));

        Assert.Equal(StockErrors.InsufficientOnHand, exception.Error);
    }

    [Fact]
    public void Apply_Release_Below_Zero_Throws_Insufficient_Allocated()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, Now);

        var exception = Assert.Throws<StockBalanceException>(
            () => item.Apply(StockMovement.Create(item.Id, StockMovementType.Release, 5, "REL", null, null, Now), Now));

        Assert.Equal(StockErrors.InsufficientAllocated, exception.Error);
    }

    [Fact]
    public void Apply_Issue_Into_Allocated_Stock_Throws_Insufficient_Available()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, Now);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Receipt, 10, "RECV", null, null, Now), Now);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Allocate, 8, "ORD-1", null, null, Now), Now);

        var exception = Assert.Throws<StockBalanceException>(
            () => item.Apply(StockMovement.Create(item.Id, StockMovementType.Issue, 5, "ISSUE", null, null, Now), Now));

        Assert.Equal(StockErrors.InsufficientAvailable, exception.Error);
        Assert.Equal(10, item.OnHand);
        Assert.Equal(8, item.Allocated);
    }

    [Fact]
    public void Apply_Does_Not_Mutate_On_Rejected_Movement()
    {
        var item = StockItem.Create("SKU-1", WarehouseId, Now);

        Assert.Throws<StockBalanceException>(
            () => item.Apply(StockMovement.Create(item.Id, StockMovementType.Issue, 5, "ISSUE", null, null, Now), Now));

        Assert.Equal(0, item.OnHand);
        Assert.Equal(0, item.Allocated);
    }

    [Fact]
    public void Create_Normalizes_Sku_To_Upper()
    {
        var item = StockItem.Create(" sku-1 ", WarehouseId, Now);

        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(0, item.Available);
    }
}
