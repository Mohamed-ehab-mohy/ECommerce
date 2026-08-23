
namespace ECommerce.Domain.Fulfillment;

public static class FulfillmentErrors
{
    public static readonly Error TaskNotFound = new(
        "ERR_FLM_001",
        "The fulfillment task was not found.",
        ErrorType.NotFound);

    public static readonly Error InvalidState = new(
        "ERR_FLM_002",
        "The fulfillment task state does not allow this operation.",
        ErrorType.Conflict);

    public static readonly Error NotQueued = new(
        "ERR_FLM_003",
        "The fulfillment task must be queued before this operation.",
        ErrorType.Conflict);

    public static readonly Error NotAssigned = new(
        "ERR_FLM_004",
        "The fulfillment task must be assigned before this operation.",
        ErrorType.Conflict);

    public static readonly Error NotPicking = new(
        "ERR_FLM_005",
        "The fulfillment task must be in picking before this operation.",
        ErrorType.Conflict);

    public static readonly Error NotPacked = new(
        "ERR_FLM_006",
        "The fulfillment task must be packed before this operation.",
        ErrorType.Conflict);

    public static readonly Error TaskExistsForOrder = new(
        "ERR_FLM_007",
        "A fulfillment task already exists for this order.",
        ErrorType.Conflict);

    public static readonly Error OrderNotReady = new(
        "ERR_FLM_008",
        "The order is not ready for fulfillment.",
        ErrorType.Conflict);

    public static readonly Error WarehouseNotFound = new(
        "ERR_FLM_009",
        "The warehouse was not found.",
        ErrorType.NotFound);

    public static readonly Error CarrierUnavailable = new(
        "ERR_FLM_010",
        "The carrier is unavailable. Try again later.",
        ErrorType.BadGateway);

    public static readonly Error InvalidSplit = new(
        "ERR_FLM_011",
        "The split must move at least one item and leave at least one item on the original task.",
        ErrorType.Conflict);
}
