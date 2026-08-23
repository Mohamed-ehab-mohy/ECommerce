namespace ECommerce.Domain.Fulfillment;

public sealed class TrackingUpdate
{
    private TrackingUpdate()
    {
    }

    private TrackingUpdate(Guid shipmentId, ShipmentStatus status, DateTime occurredAt, string? note)
    {
        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        Status = status;
        OccurredAt = occurredAt;
        Note = note;
    }

    public Guid Id { get; private set; }

    public Guid ShipmentId { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public string? Note { get; private set; }

    public static TrackingUpdate Create(Guid shipmentId, ShipmentStatus status, DateTime occurredAt, string? note) =>
        new(shipmentId, status, occurredAt, note);
}
