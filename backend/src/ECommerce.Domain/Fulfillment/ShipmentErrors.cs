
namespace ECommerce.Domain.Fulfillment;

public static class ShipmentErrors
{
    public static readonly Error ShipmentNotFound = new(
        "ERR_SHP_001",
        "The shipment was not found.",
        ErrorType.NotFound);

    public static readonly Error AlreadyDelivered = new(
        "ERR_SHP_002",
        "The shipment has already been delivered.",
        ErrorType.Conflict);

    public static readonly Error InvalidTransition = new(
        "ERR_SHP_003",
        "The tracking update is not allowed from the current shipment state.",
        ErrorType.Conflict);

    public static readonly Error UnknownCarrier = new(
        "ERR_SHP_004",
        "The carrier is not registered.",
        ErrorType.BadRequest);
}
