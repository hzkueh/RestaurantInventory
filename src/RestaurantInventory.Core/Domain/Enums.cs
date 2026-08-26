namespace RestaurantInventory.Core.Domain;

/// <summary>
/// The single unit an <see cref="InventoryItem"/> is counted in. Fixed per item;
/// the system does no conversion between units.
/// </summary>
public enum UnitOfMeasure
{
    Kg,
    G,
    L,
    Ml,
    Each
}

/// <summary>
/// The kind of a <see cref="StockMovement"/>.
/// </summary>
public enum MovementType
{
    /// <summary>A delivery in — increases stock.</summary>
    Received,

    /// <summary>A loss out — decreases stock; carries a <see cref="WasteReason"/>.</summary>
    Wasted,

    /// <summary>A +/- physical stock-count correction.</summary>
    Adjusted
}

/// <summary>
/// The required reason attached to every <see cref="MovementType.Wasted"/> movement.
/// </summary>
public enum WasteReason
{
    Spoiled,
    Expired,
    Spilled,
    Overproduction,
    Other
}
