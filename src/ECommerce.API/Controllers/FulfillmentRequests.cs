namespace ECommerce.API.Controllers;

public sealed record CreateFulfillmentTaskRequest(
    Guid OrderId,
    Guid WarehouseId,
    int Priority,
    string? Zone);

public sealed record AssignFulfillmentTaskRequest(
    Guid AssigneeId);

public sealed record CreateShipmentRequest(
    Guid TaskId,
    string CarrierKey,
    string DestinationCountry,
    string DestinationPostalCode,
    int WeightGrams,
    string Currency);

public sealed record ApplyShipmentTrackingRequest(
    string Status);
