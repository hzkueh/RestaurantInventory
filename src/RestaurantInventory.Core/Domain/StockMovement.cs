namespace RestaurantInventory.Core.Domain;

/// <summary>
/// An append-only record of a single change to an InventoryItem's stock. The source
/// of truth for quantity (see ADR-0001): never edited or deleted; mistakes are
/// corrected by posting a compensating movement.
/// </summary>
public class StockMovement
{
    // Parameterless constructor for EF Core materialisation.
    private StockMovement() { }

    public StockMovement(
        int inventoryItemId,
        MovementType type,
        decimal quantity,
        DateTimeOffset timestamp,
        string? reason = null,
        WasteReason? wasteReason = null,
        string? note = null)
    {
        InventoryItemId = inventoryItemId;
        Type = type;
        Quantity = quantity;
        Timestamp = timestamp;
        Reason = reason;
        WasteReason = wasteReason;
        Note = note;
    }

    public int Id { get; private set; }

    public int InventoryItemId { get; private set; }

    public InventoryItem? InventoryItem { get; private set; }

    public MovementType Type { get; private set; }

    /// <summary>
    /// The magnitude/sign of the change as posted. Received and Wasted are stored as
    /// positive; Adjusted may be positive or negative. The service interprets sign
    /// when updating QuantityOnHand.
    /// </summary>
    public decimal Quantity { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// Free-text reason for the movement. Optional at the schema level; the inventory
    /// service requires it for <see cref="MovementType.Adjusted"/> movements.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>Required for <see cref="MovementType.Wasted"/>; null otherwise.</summary>
    public WasteReason? WasteReason { get; private set; }

    /// <summary>Optional free-text context for a Waste.</summary>
    public string? Note { get; private set; }
}
