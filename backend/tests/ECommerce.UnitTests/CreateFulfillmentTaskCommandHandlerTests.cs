using ECommerce.Domain.Catalog;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Handlers;

namespace ECommerce.UnitTests;

public sealed class CreateFulfillmentTaskCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeProductRepository _products = new();

    private readonly FakeWarehouseRepository _warehouses = new();

    private readonly FakeFulfillmentTaskRepository _tasks = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateFulfillmentTaskCommandHandler Handler =>
        new(
            _orders,
            _products,
            _warehouses,
            _tasks,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new CreateFulfillmentTaskCommandValidator());

    private Product CreateProduct(string sku = "SKU-1") =>
        Product.Create(
            sku, "widget", "en", "Widget", null,
            "USD", 20.00m, 15.00m, null, null, false, ProductStatus.Active, UtcNow);

    private Order CreateAwaitingFulfillmentOrder(Product product)
    {
        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 2, null)],
            new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m, 0m));

        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "ahmed@example.com",
            "USD",
            "E-20260813-0001",
            snapshot,
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            "standard",
            Guid.NewGuid(),
            UtcNow);

        order.MarkBackordered([(product.Id, product.Sku, 2)], UtcNow);
        order.FillBackorderItems(product.Sku, 2, UtcNow);

        return order;
    }

    private static Warehouse CreateWarehouse() =>
        Warehouse.Create("DXB", "Dubai Central", "1 Sheikh Zayed Rd", "Asia/Dubai", WarehouseStatus.Active, UtcNow);

    [Fact]
    public async Task Create_Adds_Task_With_Order_Items()
    {
        var product = CreateProduct();
        _products.Add(product);
        var order = CreateAwaitingFulfillmentOrder(product);
        _orders.Add(order);
        var warehouse = CreateWarehouse();
        _warehouses.Add(warehouse);

        var result = await Handler.Handle(
            new CreateFulfillmentTaskCommand(order.Id, warehouse.Id, 5, "A"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var task = Assert.Single(_tasks.Tasks);
        Assert.Equal(order.Id, task.OrderId);
        Assert.Equal(warehouse.Id, task.WarehouseId);
        var line = Assert.Single(task.Items);
        Assert.Equal(product.Sku, line.Sku);
        Assert.Equal(2, line.Quantity);
        Assert.Equal("Queued", result.Value.Status);
    }

    [Fact]
    public async Task Create_Duplicate_For_Order_Returns_Conflict()
    {
        var product = CreateProduct();
        _products.Add(product);
        var order = CreateAwaitingFulfillmentOrder(product);
        _orders.Add(order);
        _tasks.Add(ECommerce.Domain.Fulfillment.FulfillmentTask.Create(order.Id, WarehouseId, 1, UtcNow, null));

        var result = await Handler.Handle(
            new CreateFulfillmentTaskCommand(order.Id, WarehouseId, 1, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ECommerce.Domain.Fulfillment.FulfillmentErrors.TaskExistsForOrder, result.Error);
    }

    [Fact]
    public async Task Create_Unknown_Order_Returns_NotFound()
    {
        var result = await Handler.Handle(
            new CreateFulfillmentTaskCommand(Guid.NewGuid(), WarehouseId, 1, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task Create_Order_Not_Ready_Returns_Conflict()
    {
        var product = CreateProduct();
        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 1, null)],
            new TotalsSnapshot(15.00m, 0m, 0m, 9.90m, 0m, 24.90m, 0m));

        var order = Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, "ahmed@example.com", "USD",
            "E-20260813-0002", snapshot,
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            "standard", Guid.NewGuid(), UtcNow);
        _orders.Add(order);

        var result = await Handler.Handle(
            new CreateFulfillmentTaskCommand(order.Id, WarehouseId, 1, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ECommerce.Domain.Fulfillment.FulfillmentErrors.OrderNotReady, result.Error);
    }

    [Fact]
    public async Task Create_Unknown_Warehouse_Returns_NotFound()
    {
        var product = CreateProduct();
        _products.Add(product);
        var order = CreateAwaitingFulfillmentOrder(product);
        _orders.Add(order);

        var result = await Handler.Handle(
            new CreateFulfillmentTaskCommand(order.Id, WarehouseId, 1, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ECommerce.Domain.Fulfillment.FulfillmentErrors.WarehouseNotFound, result.Error);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
