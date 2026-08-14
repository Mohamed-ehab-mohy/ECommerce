using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Handlers;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UnitTests;

public sealed class ApplyShipmentTrackingCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    private readonly FakeShipmentRepository _shipments = new();

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private ApplyShipmentTrackingCommandHandler Handler =>
        new(_shipments, _orders, _unitOfWork, new FixedTimeProvider(UtcNow), new ApplyShipmentTrackingCommandValidator());

    private static Order CreateShippedOrder()
    {
        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
            new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m, 0m));

        var order = Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, "ahmed@example.com", "USD",
            "E-20260813-0001", snapshot,
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            "standard", Guid.NewGuid(), UtcNow);
        order.MarkBackordered([(Guid.NewGuid(), "SKU-1", 2)], UtcNow);
        order.FillBackorderItems("SKU-1", 2, UtcNow);
        order.StartFulfillment("user", null, null, UtcNow);
        order.MarkPacked("user", null, null, UtcNow);
        order.Ship("dhl", ["TRK-1"], "user", null, null, UtcNow);

        return order;
    }

    private Shipment SeedShippedOrder()
    {
        var order = CreateShippedOrder();
        _orders.Add(order);

        var shipment = Shipment.Create(order.Id, Guid.NewGuid(), "dhl", "TRK-1", null, UtcNow);
        _shipments.Add(shipment);
        return shipment;
    }

    [Fact]
    public async Task Apply_InTransit_Updates_Shipment()
    {
        var shipment = SeedShippedOrder();

        var result = await Handler.Handle(
            new ApplyShipmentTrackingCommand(shipment.Id, "InTransit"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(2, shipment.Updates.Count);
    }

    [Fact]
    public async Task Apply_Delivered_Transitions_Order_To_Delivered()
    {
        var shipment = SeedShippedOrder();
        var order = _orders.Orders[0];
        shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow);
        shipment.ApplyTrackingUpdate(ShipmentStatus.OutForDelivery, UtcNow);

        var result = await Handler.Handle(
            new ApplyShipmentTrackingCommand(shipment.Id, "Delivered"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(shipment.DeliveredAt);
    }

    [Fact]
    public async Task Apply_Invalid_Transition_Returns_Conflict()
    {
        var shipment = SeedShippedOrder();

        var result = await Handler.Handle(
            new ApplyShipmentTrackingCommand(shipment.Id, "OutForDelivery"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShipmentErrors.InvalidTransition, result.Error);
    }

    [Fact]
    public async Task Apply_Unknown_Shipment_Returns_NotFound()
    {
        var result = await Handler.Handle(
            new ApplyShipmentTrackingCommand(Guid.NewGuid(), "InTransit"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ShipmentErrors.ShipmentNotFound, result.Error);
    }

    [Fact]
    public async Task Apply_Delivered_First_Of_Split_Shipments_Keeps_Order_Shipped()
    {
        var order = CreateShippedOrder();
        _orders.Add(order);

        var first = Shipment.Create(order.Id, Guid.NewGuid(), "dhl", "TRK-1", null, UtcNow);
        var second = Shipment.Create(order.Id, Guid.NewGuid(), "aramex", "TRK-2", null, UtcNow);
        _shipments.Add(first);
        _shipments.Add(second);

        var delivered = await DeliverAsync(first);

        Assert.True(delivered.IsSuccess, delivered.Error.Description);
        Assert.Equal(ShipmentStatus.Delivered, first.Status);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public async Task Apply_Delivered_Last_Of_Split_Shipments_Transitions_Order_To_Delivered()
    {
        var order = CreateShippedOrder();
        _orders.Add(order);

        var first = Shipment.Create(order.Id, Guid.NewGuid(), "dhl", "TRK-1", null, UtcNow);
        var second = Shipment.Create(order.Id, Guid.NewGuid(), "aramex", "TRK-2", null, UtcNow);
        _shipments.Add(first);
        _shipments.Add(second);

        await DeliverAsync(first);

        var result = await DeliverAsync(second);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ShipmentStatus.Delivered, second.Status);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    private async Task<Result<ShipmentResponse>> DeliverAsync(Shipment shipment)
    {
        shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow);
        shipment.ApplyTrackingUpdate(ShipmentStatus.OutForDelivery, UtcNow);

        return await Handler.Handle(
            new ApplyShipmentTrackingCommand(shipment.Id, "Delivered"),
            CancellationToken.None);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
