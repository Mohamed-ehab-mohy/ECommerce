using ECommerce.Domain.Inventory;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Inventory.Handlers;
using ECommerce.UseCases.Inventory.Queries;

namespace ECommerce.UnitTests;

public sealed class StockQueryHandlerTests
{
    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly FakeStockRepository _stock = new();

    private ListStockItemsQueryHandler ListHandler =>
        new(_stock, new ListStockItemsQueryValidator());

    private GetStockItemQueryHandler GetHandler =>
        new(_stock, new GetStockItemQueryValidator());

    private ListStockMovementsQueryHandler MovementsHandler =>
        new(_stock, new ListStockMovementsQueryValidator());

    [Fact]
    public async Task ListStockItems_Returns_Paged_Items()
    {
        _stock.Items.Add(CreateItem("SKU-B", WarehouseId, 5, 2));
        _stock.Items.Add(CreateItem("SKU-A", WarehouseId, 1, 0));

        var result = await ListHandler.Handle(new ListStockItemsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal("SKU-A", result.Value.Items[0].Sku);
        Assert.Equal("SKU-B", result.Value.Items[1].Sku);
        Assert.Equal(3, result.Value.Items[1].Available);
    }

    [Fact]
    public async Task ListStockItems_Filters_By_Warehouse()
    {
        _stock.Items.Add(CreateItem("SKU-A", WarehouseId, 1, 0));
        _stock.Items.Add(CreateItem("SKU-B", Guid.NewGuid(), 1, 0));

        var result = await ListHandler.Handle(new ListStockItemsQuery(1, 20, WarehouseId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("SKU-A", Assert.Single(result.Value.Items).Sku);
    }

    [Fact]
    public async Task ListStockItems_With_Invalid_Pagination_Returns_Validation_Failure()
    {
        var result = await ListHandler.Handle(new ListStockItemsQuery(0, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task GetStockItem_Returns_Item()
    {
        var item = CreateItem("SKU-A", WarehouseId, 10, 4);
        _stock.Items.Add(item);

        var result = await GetHandler.Handle(new GetStockItemQuery(item.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SKU-A", result.Value.Sku);
        Assert.Equal(6, result.Value.Available);
    }

    [Fact]
    public async Task GetStockItem_With_Unknown_Id_Returns_NotFound()
    {
        var result = await GetHandler.Handle(new GetStockItemQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(StockErrors.StockItemNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task ListStockMovements_Returns_Movements_Most_Recent_First()
    {
        var item = CreateItem("SKU-A", WarehouseId, 0, 0);
        _stock.Items.Add(item);

        _stock.Movements.Add(CreateMovement(item.Id, StockMovementType.Receipt, 5, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)));
        _stock.Movements.Add(CreateMovement(item.Id, StockMovementType.Issue, 2, new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc)));

        var result = await MovementsHandler.Handle(new ListStockMovementsQuery(item.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal("Issue", result.Value.Items[0].Type);
        Assert.Equal("Receipt", result.Value.Items[1].Type);
    }

    [Fact]
    public async Task ListStockMovements_With_Unknown_StockItem_Returns_Empty()
    {
        var result = await MovementsHandler.Handle(
            new ListStockMovementsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }

    private static StockItem CreateItem(string sku, Guid warehouseId, int onHand, int allocated)
    {
        var item = StockItem.Create(sku, warehouseId, DateTime.UtcNow);
        if (onHand != 0)
        {
            item.Apply(StockMovement.Create(item.Id, StockMovementType.Adjustment, onHand, "SEED", null, null, DateTime.UtcNow), DateTime.UtcNow);
        }

        if (allocated > 0)
        {
            item.Apply(StockMovement.Create(item.Id, StockMovementType.Allocate, allocated, "SEED", null, null, DateTime.UtcNow), DateTime.UtcNow);
        }

        return item;
    }

    private static StockMovement CreateMovement(Guid stockItemId, StockMovementType type, int quantity, DateTime createdAt) =>
        StockMovement.Create(stockItemId, type, quantity, "SEED", null, null, createdAt);
}
