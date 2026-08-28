using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantInventory.Core.Domain;

/// <summary>
/// A consumable the kitchen holds in stock. Its <see cref="QuantityOnHand"/> is a
/// derived-and-snapshotted cache of the sum of its <see cref="StockMovement"/>s
/// (see ADR-0001) — never an independently editable authoritative quantity.
/// </summary>
public class InventoryItem
{
    /// <summary>Longest an item name may be — mirrors the persistence column (see InventoryDbContext).</summary>
    public const int MaxNameLength = 200;

    private readonly List<StockMovement> _movements = new();

    // Parameterless constructor for EF Core materialisation.
    private InventoryItem() { }

    public InventoryItem(string name, UnitOfMeasure unitOfMeasure, decimal unitCost, decimal reorderLevel)
    {
        // The unit is the one field with no later mutator: it is set here and never again, so
        // historical StockMovements stay meaningful in a single unit (user story 8). The three
        // editable fields go through UpdateDetails so their invariants live in exactly one place.
        UnitOfMeasure = unitOfMeasure;
        UpdateDetails(name, unitCost, reorderLevel);
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Fixed after creation — StockMovement history stays meaningful in one unit.</summary>
    public UnitOfMeasure UnitOfMeasure { get; private set; }

    public decimal UnitCost { get; private set; }

    /// <summary>Manager-set threshold; the item is in Shortage when QuantityOnHand &lt;= this.</summary>
    public decimal ReorderLevel { get; private set; }

    /// <summary>
    /// Cached sum of this item's StockMovements. Updated only by the inventory
    /// service inside the same operation that appends a movement, so reads never
    /// diverge from the ledger. Not settable from outside the Core assembly.
    /// </summary>
    public decimal QuantityOnHand { get; internal set; }

    public IReadOnlyCollection<StockMovement> Movements => _movements;

    /// <summary>
    /// Computed, never stored (ADR-0001): the item needs reordering when its cached
    /// quantity has fallen to or below the manager-set <see cref="ReorderLevel"/>.
    /// The boundary is inclusive — exactly at the level counts as Shortage.
    /// </summary>
    [NotMapped]
    public bool IsInShortage => QuantityOnHand <= ReorderLevel;

    /// <summary>
    /// Sets the three editable pieces of item metadata — name, <see cref="UnitCost"/>, and
    /// manager-set <see cref="ReorderLevel"/> — validating them together. This is the only way to
    /// change them, at creation or later; <see cref="UnitOfMeasure"/> is deliberately not among
    /// them (user story 8) and <see cref="QuantityOnHand"/> is left to the ledger.
    /// </summary>
    /// <exception cref="InvalidItemException">
    /// The name is blank, or a cost/level is negative; no field is changed.
    /// </exception>
    public void UpdateDetails(string name, decimal unitCost, decimal reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidItemException("An item name is required.");
        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new InvalidItemException($"An item name cannot exceed {MaxNameLength} characters.");
        if (unitCost < 0)
            throw new InvalidItemException("Unit cost cannot be negative.");
        if (reorderLevel < 0)
            throw new InvalidItemException("Reorder level cannot be negative.");

        Name = trimmed;
        UnitCost = unitCost;
        ReorderLevel = reorderLevel;
    }

    /// <summary>
    /// Validates and records a single change to this item's stock, appending it to the
    /// append-only ledger and updating the cached <see cref="QuantityOnHand"/> in the
    /// same operation so reads and the cache can never diverge (ADR-0001).
    /// </summary>
    /// <param name="timestamp">
    /// When the movement occurred. The application service stamps this in UTC so that
    /// string ordering of the persisted column matches chronological order.
    /// </param>
    /// <returns>The movement that was appended.</returns>
    /// <exception cref="InvalidMovementException">
    /// The movement breaks a ledger invariant; nothing is appended and the cache is untouched.
    /// </exception>
    public StockMovement PostMovement(
        MovementType type,
        decimal quantity,
        DateTimeOffset timestamp,
        string? reason = null,
        WasteReason? wasteReason = null,
        string? note = null)
    {
        Validate(type, quantity, reason, wasteReason);

        var movement = StockMovement.CreateFor(this, type, quantity, timestamp, reason, wasteReason, note);
        _movements.Add(movement);
        QuantityOnHand += SignedDelta(type, quantity);
        return movement;
    }

    /// <summary>The effect a valid movement has on <see cref="QuantityOnHand"/>.</summary>
    private static decimal SignedDelta(MovementType type, decimal quantity) => type switch
    {
        MovementType.Received => quantity,   // stored positive, adds stock
        MovementType.Wasted => -quantity,    // stored positive, removes stock
        MovementType.Adjusted => quantity,   // already signed
        _ => throw new InvalidMovementException($"Unknown movement type '{type}'."),
    };

    private static void Validate(MovementType type, decimal quantity, string? reason, WasteReason? wasteReason)
    {
        switch (type)
        {
            case MovementType.Received:
                if (quantity <= 0)
                    throw new InvalidMovementException("Received quantity must be positive.");
                if (wasteReason is not null)
                    throw new InvalidMovementException("A WasteReason applies only to Wasted movements.");
                break;

            case MovementType.Wasted:
                if (quantity <= 0)
                    throw new InvalidMovementException("Wasted quantity must be positive.");
                if (wasteReason is null)
                    throw new InvalidMovementException("A Wasted movement requires a WasteReason.");
                break;

            case MovementType.Adjusted:
                if (quantity == 0)
                    throw new InvalidMovementException("Adjusted quantity must be non-zero.");
                if (string.IsNullOrWhiteSpace(reason))
                    throw new InvalidMovementException("An Adjusted movement requires a reason.");
                if (wasteReason is not null)
                    throw new InvalidMovementException("A WasteReason applies only to Wasted movements.");
                break;

            default:
                throw new InvalidMovementException($"Unknown movement type '{type}'.");
        }
    }
}
