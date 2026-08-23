using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Fulfillment;

public sealed class Shipment : BaseEntity<Guid>
{
    private readonly List<TrackingUpdate> _updates = [];

    private Shipment()
    {
        CarrierKey = string.Empty;
        TrackingNumber = string.Empty;
    }

    public Guid OrderId { get; private set; }

    public Guid FulfillmentTaskId { get; private set; }

    public string CarrierKey { get; private set; }

    public string TrackingNumber { get; private set; }

    public string? LabelUrl { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTime ShippedAt { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public IReadOnlyCollection<TrackingUpdate> Updates => _updates;

    public static Shipment Create(
        Guid orderId,
        Guid fulfillmentTaskId,
        string carrierKey,
        string trackingNumber,
        string? labelUrl,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrierKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FulfillmentTaskId = fulfillmentTaskId,
            CarrierKey = carrierKey,
            TrackingNumber = trackingNumber.Trim(),
            LabelUrl = string.IsNullOrWhiteSpace(labelUrl) ? null : labelUrl.Trim(),
            Status = ShipmentStatus.Created,
            ShippedAt = utcNow,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        shipment._updates.Add(TrackingUpdate.Create(shipment.Id, ShipmentStatus.Created, utcNow, null));

        shipment.AddDomainEvent(new ShipmentCreated(
            shipment.Id,
            orderId,
            carrierKey,
            shipment.TrackingNumber));

        return shipment;
    }

    public Result ApplyTrackingUpdate(ShipmentStatus status, DateTime utcNow)
    {
        if (Status == ShipmentStatus.Delivered)
        {
            return ShipmentErrors.AlreadyDelivered;
        }

        if (!CanTransitionTo(status))
        {
            return ShipmentErrors.InvalidTransition;
        }

        _updates.Add(TrackingUpdate.Create(Id, status, utcNow, null));
        Status = status;
        if (status == ShipmentStatus.Delivered)
        {
            DeliveredAt = utcNow;
        }

        UpdatedAt = utcNow;

        AddDomainEvent(new ShipmentStatusChanged(Id, OrderId, CarrierKey, TrackingNumber, Status));

        if (status == ShipmentStatus.Delivered)
        {
            AddDomainEvent(new ShipmentDelivered(Id, OrderId, CarrierKey, TrackingNumber));
        }

        return Result.Success();
    }

    private bool CanTransitionTo(ShipmentStatus next) =>
        (Status, next) switch
        {
            (ShipmentStatus.Created, ShipmentStatus.InTransit) => true,
            (ShipmentStatus.Created, ShipmentStatus.Exception) => true,
            (ShipmentStatus.InTransit, ShipmentStatus.OutForDelivery) => true,
            (ShipmentStatus.InTransit, ShipmentStatus.Exception) => true,
            (ShipmentStatus.OutForDelivery, ShipmentStatus.Delivered) => true,
            (ShipmentStatus.OutForDelivery, ShipmentStatus.Exception) => true,
            (ShipmentStatus.Exception, ShipmentStatus.InTransit) => true,
            _ => false
        };
}
