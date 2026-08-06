using ECommerce.Domain.Common;

namespace ECommerce.Domain.Inventory;

public sealed class StockMovement : BaseEntity<Guid>
{
    private StockMovement()
    {
        Type = StockMovementType.Receipt;
        Reason = string.Empty;
    }

    public Guid StockItemId { get; private set; }

    public StockMovementType Type { get; private set; }

    public int Quantity { get; private set; }

    public int OnHandDelta { get; private set; }

    public int AllocatedDelta { get; private set; }

    public string Reason { get; private set; }

    public string? Reference { get; private set; }

    public string? Note { get; private set; }

    public static StockMovement Create(
        Guid stockItemId,
        StockMovementType type,
        int quantity,
        string reason,
        string? reference,
        string? note,
        DateTime utcNow)
    {
        if (quantity == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A movement quantity must not be zero.");
        }

        if (type != StockMovementType.Adjustment && quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Only adjustments can use a negative quantity.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A movement must have a reason code.", nameof(reason));
        }

        (int onHandDelta, int allocatedDelta) = ComputeDeltas(type, quantity);

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            StockItemId = stockItemId,
            Type = type,
            Quantity = quantity,
            OnHandDelta = onHandDelta,
            AllocatedDelta = allocatedDelta,
            Reason = reason.Trim(),
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    private static (int OnHandDelta, int AllocatedDelta) ComputeDeltas(StockMovementType type, int quantity)
    {
        return type switch
        {
            StockMovementType.Receipt => (quantity, 0),
            StockMovementType.Issue => (-quantity, 0),
            StockMovementType.Adjustment => (quantity, 0),
            StockMovementType.Allocate => (0, quantity),
            StockMovementType.Release => (0, -quantity),
            StockMovementType.Fulfill => (-quantity, -quantity),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
