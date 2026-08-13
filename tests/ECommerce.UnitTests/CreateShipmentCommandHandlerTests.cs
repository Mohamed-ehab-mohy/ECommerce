using ECommerce.Domain.Catalog;
using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Handlers;
using ECommerce.UseCases.Fulfillment.Shipping;

namespace ECommerce.UnitTests;

public sealed class CreateShipmentCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private readonly FakeFulfillmentTaskRepository _tasks = new();

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeShipmentRepository _shipments = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakeCarrierAdapter _dhl = new("dhl");

    private readonly FakeCarrierAdapter _aramex = new("aramex");

    private CreateShipmentCommandHandler Handler =>
        new(
            _tasks,
            _orders,
            _shipments,
            [_dhl, _aramex],
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new CreateShipmentCommandValidator());

    private (FulfillmentTask Task, Order Order) SeedPackedTask()
    {
        var product = Product.Create(
            "SKU-1", "widget", "en", "Widget", null,
            "USD", 20.00m, 15.00m, null, null, false, ProductStatus.Active, UtcNow);

        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, 2, null)],
            new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m));

        var order = Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, "ahmed@example.com", "USD",
            "E-20260813-0001", snapshot,
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            "standard", Guid.NewGuid(), UtcNow);
        order.MarkBackordered([(product.Id, product.Sku, 2)], UtcNow);
        order.FillBackorderItems(product.Sku, 2, UtcNow);
        order.StartFulfillment("user", null, null, UtcNow);
        order.MarkPacked("user", null, null, UtcNow);

        var task = FulfillmentTask.Create(order.Id, WarehouseId, 1, UtcNow, "A");
        task.AddItem(product.Id, product.Sku, 2, null);
        task.Assign(Guid.NewGuid(), UtcNow);
        task.StartPicking(UtcNow);
        task.MarkPacked(UtcNow);

        _orders.Add(order);
        _tasks.Add(task);
        return (task, order);
    }

    private static CreateShipmentCommand CreateCommand(Guid taskId) =>
        new(taskId, "dhl", "SA", "11461", 1200, "SAR");

    [Fact]
    public async Task Ship_Creates_Shipment_And_Transitions_Task_And_Order()
    {
        var (task, order) = SeedPackedTask();

        var result = await Handler.Handle(CreateCommand(task.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var shipment = Assert.Single(_shipments.Shipments);
        Assert.Equal(order.Id, shipment.OrderId);
        Assert.Equal(task.Id, shipment.FulfillmentTaskId);
        Assert.Equal("dhl", shipment.CarrierKey);
        Assert.Equal("TRK-dhl-1", shipment.TrackingNumber);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Equal(FulfillmentTaskStatus.Shipped, task.Status);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(1, _dhl.CreateCallCount);
        Assert.Equal(0, _aramex.CreateCallCount);
        Assert.Equal("TRK-dhl-1", result.Value.TrackingNumber);
    }

    [Fact]
    public async Task Ship_Unknown_Task_Returns_NotFound()
    {
        var result = await Handler.Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.TaskNotFound, result.Error);
    }

    [Fact]
    public async Task Ship_Unpacked_Task_Returns_NotPacked()
    {
        var (_, order) = SeedPackedTask();
        var queuedTask = FulfillmentTask.Create(order.Id, WarehouseId, 1, UtcNow, "A");
        queuedTask.AddItem(Guid.NewGuid(), "SKU-1", 2, null);
        _tasks.Add(queuedTask);

        var result = await Handler.Handle(CreateCommand(queuedTask.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.NotPacked, result.Error);
        Assert.Empty(_shipments.Shipments);
    }

    [Fact]
    public async Task Ship_Unknown_Carrier_Returns_UnknownCarrier()
    {
        var (task, _) = SeedPackedTask();

        var result = await Handler.Handle(
            new CreateShipmentCommand(task.Id, "fedex", "SA", "11461", 1200, "SAR"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShipmentErrors.UnknownCarrier, result.Error);
        Assert.Empty(_shipments.Shipments);
    }

    [Fact]
    public async Task Ship_Carrier_Error_Returns_CarrierUnavailable()
    {
        var (task, _) = SeedPackedTask();
        _dhl.ThrowOnCreate = true;

        var result = await Handler.Handle(CreateCommand(task.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrors.CarrierUnavailable, result.Error);
        Assert.Empty(_shipments.Shipments);
    }

    [Fact]
    public async Task Ship_From_Aramex_Uses_Aramex_Adapter()
    {
        var (task, _) = SeedPackedTask();

        var result = await Handler.Handle(
            new CreateShipmentCommand(task.Id, "aramex", "SA", "11461", 1200, "SAR"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("aramex", Assert.Single(_shipments.Shipments).CarrierKey);
        Assert.Equal(1, _aramex.CreateCallCount);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
