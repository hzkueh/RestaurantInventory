namespace RestaurantInventory.Core.Domain;

/// <summary>
/// A consumable the kitchen holds in stock. Its <see cref="QuantityOnHand"/> is a
/// derived-and-snapshotted cache of the sum of its <see cref="StockMovement"/>s
/// (see ADR-0001) — never an independently editable authoritative quantity.
/// </summary>
public class InventoryItem
{
    private readonly List<StockMovement> _movements = new();

    // Parameterless constructor for EF Core materialisation.
    private InventoryItem() { }

    public InventoryItem(string name, UnitOfMeasure unitOfMeasure, decimal unitCost, decimal reorderLevel)
    {
        Name = name;
        UnitOfMeasure = unitOfMeasure;
        UnitCost = unitCost;
        ReorderLevel = reorderLevel;
    }

    public int Id { get; private set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Fixed after creation — StockMovement history stays meaningful in one unit.</summary>
    public UnitOfMeasure UnitOfMeasure { get; private set; }

    public decimal UnitCost { get; set; }

    /// <summary>Manager-set threshold; the item is in Shortage when QuantityOnHand &lt;= this.</summary>
    public decimal ReorderLevel { get; set; }

    /// <summary>
    /// Cached sum of this item's StockMovements. Updated only by the inventory
    /// service inside the same operation that appends a movement, so reads never
    /// diverge from the ledger. Not settable from outside the Core assembly.
    /// </summary>
    public decimal QuantityOnHand { get; internal set; }

    public IReadOnlyCollection<StockMovement> Movements => _movements;
}
