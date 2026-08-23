using ECommerce.Domain.Fulfillment;

namespace ECommerce.UseCases.Fulfillment.Responses;

public sealed record TrackingUpdateResponse(
    Guid Id,
    string Status,
    DateTime OccurredAt,
    string? Note);

public sealed record ShipmentResponse(
    Guid ShipmentId,
    Guid OrderId,
    Guid FulfillmentTaskId,
    string CarrierKey,
    string TrackingNumber,
    string? LabelUrl,
    string Status,
    DateTime ShippedAt,
    DateTime? DeliveredAt,
    IReadOnlyList<TrackingUpdateResponse> Updates)
{
    public static ShipmentResponse From(Shipment shipment) =>
        new(
            shipment.Id,
            shipment.OrderId,
            shipment.FulfillmentTaskId,
            shipment.CarrierKey,
            shipment.TrackingNumber,
            shipment.LabelUrl,
            shipment.Status.ToString(),
            shipment.ShippedAt,
            shipment.DeliveredAt,
            shipment.Updates
                .OrderBy(update => update.OccurredAt)
                .Select(update => new TrackingUpdateResponse(
                    update.Id,
                    update.Status.ToString(),
                    update.OccurredAt,
                    update.Note))
                .ToList());
}
