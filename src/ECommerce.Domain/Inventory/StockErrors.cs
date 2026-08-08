using ECommerce.Shared.Errors;

namespace ECommerce.Domain.Inventory;

public static class StockErrors
{
    public static readonly Error StockItemNotFound = new(
        "Stock.StockItemNotFound",
        "The stock item was not found.",
        ErrorType.NotFound);

    public static readonly Error InsufficientOnHand = new(
        "Stock.InsufficientOnHand",
        "The movement would make on-hand stock negative.",
        ErrorType.Conflict);

    public static readonly Error InsufficientAllocated = new(
        "Stock.InsufficientAllocated",
        "The movement would make allocated stock negative.",
        ErrorType.Conflict);

    public static readonly Error InsufficientAvailable = new(
        "Stock.InsufficientAvailable",
        "The movement would make available stock negative.",
        ErrorType.Conflict);

    public static readonly Error AllocationFailed = new(
        "ERR_STK_001",
        "Insufficient stock to complete the order.",
        ErrorType.Conflict);

    public static readonly Error ApprovalRequired = new(
        "Stock.ApprovalRequired",
        "Negative adjustments require approval.",
        ErrorType.Validation);

    public static readonly Error SameWarehouseTransfer = new(
        "Stock.SameWarehouseTransfer",
        "Source and target warehouses must differ.",
        ErrorType.Validation);
}
