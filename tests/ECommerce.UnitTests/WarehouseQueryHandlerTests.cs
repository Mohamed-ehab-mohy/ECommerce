using ECommerce.Domain.Inventory;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Inventory.Handlers;
using ECommerce.UseCases.Inventory.Queries;

namespace ECommerce.UnitTests;

public sealed class WarehouseQueryHandlerTests
{
    private readonly FakeWarehouseRepository _warehouses = new();

    private ListWarehousesQueryHandler ListHandler =>
        new(_warehouses, new ListWarehousesQueryValidator());

    private GetWarehouseQueryHandler GetHandler =>
        new(_warehouses, new GetWarehouseQueryValidator());

    [Fact]
    public async Task ListWarehouses_Returns_Paged_Items_Sorted_By_Code()
    {
        _warehouses.Warehouses.Add(CreateWarehouse("ZRH-01", "Zurich"));
        _warehouses.Warehouses.Add(CreateWarehouse("CAI-01", "Cairo"));
        _warehouses.Warehouses.Add(CreateWarehouse("DXB-01", "Dubai"));

        var result = await ListHandler.Handle(new ListWarehousesQuery(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal("CAI-01", result.Value.Items[0].Code);
        Assert.Equal("DXB-01", result.Value.Items[1].Code);
        Assert.Equal("ZRH-01", result.Value.Items[2].Code);
    }

    [Fact]
    public async Task ListWarehouses_Excludes_Deactivated_Warehouses()
    {
        var active = CreateWarehouse("CAI-01", "Cairo");
        var inactive = CreateWarehouse("DXB-01", "Dubai");
        inactive.Deactivate();
        _warehouses.Warehouses.Add(active);
        _warehouses.Warehouses.Add(inactive);

        var result = await ListHandler.Handle(new ListWarehousesQuery(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("CAI-01", Assert.Single(result.Value.Items).Code);
    }

    [Fact]
    public async Task ListWarehouses_With_Invalid_Page_Returns_Validation_Failure()
    {
        var result = await ListHandler.Handle(new ListWarehousesQuery(0, 20), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task GetWarehouse_Returns_Warehouse()
    {
        var warehouse = CreateWarehouse("CAI-01", "Cairo");
        _warehouses.Warehouses.Add(warehouse);

        var result = await GetHandler.Handle(new GetWarehouseQuery(warehouse.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CAI-01", result.Value.Code);
        Assert.Equal("Cairo", result.Value.Name);
        Assert.Equal(WarehouseStatus.Active, result.Value.Status);
    }

    [Fact]
    public async Task GetWarehouse_With_Unknown_Id_Returns_NotFound()
    {
        var result = await GetHandler.Handle(new GetWarehouseQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WarehouseErrors.WarehouseNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    private static Warehouse CreateWarehouse(string code, string name) =>
        Warehouse.Create(code, name, "Address", "UTC", WarehouseStatus.Active, DateTime.UtcNow);
}
