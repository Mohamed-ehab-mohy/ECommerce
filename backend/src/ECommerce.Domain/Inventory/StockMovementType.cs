namespace ECommerce.Domain.Inventory;

public enum StockMovementType
{
    Receipt,
    Issue,
    Adjustment,
    Allocate,
    Release,
    Fulfill
}
