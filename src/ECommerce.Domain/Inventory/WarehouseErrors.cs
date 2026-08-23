
namespace ECommerce.Domain.Inventory;

public static class WarehouseErrors
{
    public static readonly Error WarehouseNotFound = new(
        "Warehouse.WarehouseNotFound",
        "The warehouse was not found.",
        ErrorType.NotFound);

    public static readonly Error CodeAlreadyExists = new(
        "Warehouse.CodeAlreadyExists",
        "A warehouse with this code already exists.",
        ErrorType.Conflict);
}
