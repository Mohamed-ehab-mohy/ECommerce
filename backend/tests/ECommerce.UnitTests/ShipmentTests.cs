using ECommerce.Domain.Events;
using ECommerce.Domain.Fulfillment;

namespace ECommerce.UnitTests;

public sealed class ShipmentTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 16, 30, 0, DateTimeKind.Utc);

    private static readonly Guid OrderId = Guid.NewGuid();

    private static readonly Guid TaskId = Guid.NewGuid();

    private static Shipment CreateShipment() =>
        Shipment.Create(OrderId, TaskId, "dhl", "TRK-DHL-123", "https://example.com/label.pdf", UtcNow);

    [Fact]
    public void Create_Sets_Initial_State_And_Event()
    {
        var shipment = CreateShipment();

        Assert.Equal(OrderId, shipment.OrderId);
        Assert.Equal(TaskId, shipment.FulfillmentTaskId);
        Assert.Equal("dhl", shipment.CarrierKey);
        Assert.Equal("TRK-DHL-123", shipment.TrackingNumber);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Equal(UtcNow, shipment.ShippedAt);
        Assert.Null(shipment.DeliveredAt);
        Assert.Single(shipment.Updates);
        Assert.Contains(shipment.DomainEvents, domainEvent => domainEvent is ShipmentCreated);
    }

    [Fact]
    public void ApplyTracking_Created_To_InTransit()
    {
        var shipment = CreateShipment();

        var result = shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Contains(shipment.DomainEvents, domainEvent => domainEvent is ShipmentStatusChanged changed && changed.Status == ShipmentStatus.InTransit);
    }

    [Fact]
    public void ApplyTracking_Created_To_OutForDelivery_Is_Rejected()
    {
        var shipment = CreateShipment();

        var result = shipment.ApplyTrackingUpdate(ShipmentStatus.OutForDelivery, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ShipmentErrors.InvalidTransition, result.Error);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
    }

    [Fact]
    public void ApplyTracking_Full_Flow_To_Delivered()
    {
        var shipment = CreateShipment();

        Assert.True(shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow).IsSuccess);
        Assert.True(shipment.ApplyTrackingUpdate(ShipmentStatus.OutForDelivery, UtcNow).IsSuccess);
        var delivered = shipment.ApplyTrackingUpdate(ShipmentStatus.Delivered, UtcNow);

        Assert.True(delivered.IsSuccess);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.Equal(UtcNow, shipment.DeliveredAt);
        Assert.Contains(shipment.DomainEvents, domainEvent => domainEvent is ShipmentDelivered);
        Assert.Equal(4, shipment.Updates.Count);
    }

    [Fact]
    public void ApplyTracking_After_Delivered_Is_Rejected()
    {
        var shipment = CreateShipment();
        shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow);
        shipment.ApplyTrackingUpdate(ShipmentStatus.OutForDelivery, UtcNow);
        shipment.ApplyTrackingUpdate(ShipmentStatus.Delivered, UtcNow);

        var result = shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ShipmentErrors.AlreadyDelivered, result.Error);
    }

    [Fact]
    public void ApplyTracking_Exception_Allows_Recovery()
    {
        var shipment = CreateShipment();
        shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow);

        Assert.True(shipment.ApplyTrackingUpdate(ShipmentStatus.Exception, UtcNow).IsSuccess);
        Assert.True(shipment.ApplyTrackingUpdate(ShipmentStatus.InTransit, UtcNow).IsSuccess);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
    }
}
