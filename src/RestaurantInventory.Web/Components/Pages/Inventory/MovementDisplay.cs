using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>
/// Presentation helpers for a <see cref="StockMovement"/> in the item-detail audit view: the
/// signed ledger change, the reason text, and the newest-first ordering. Kept pure and in one
/// place so the audit formatting is unit-tested (it is the point of user story 14) and reusable
/// by any later movement screen.
/// </summary>
public static class MovementDisplay
{
    /// <summary>
    /// The effect the movement had on <see cref="InventoryItem.QuantityOnHand"/>, signed like a
    /// ledger entry: Received adds, Wasted subtracts, Adjusted is already signed as posted.
    /// </summary>
    public static decimal SignedDelta(StockMovement movement) => movement.Type switch
    {
        MovementType.Received => movement.Quantity,
        MovementType.Wasted => -movement.Quantity,
        _ => movement.Quantity,
    };

    /// <summary>The signed delta with its unit and a leading '+' for gains, e.g. <c>+25 kg</c> / <c>-2 kg</c>.</summary>
    public static string ChangeText(StockMovement movement, UnitOfMeasure unit)
    {
        var delta = SignedDelta(movement);
        var sign = delta > 0 ? "+" : "";
        return $"{sign}{UnitDisplay.Quantity(delta, unit)}";
    }

    /// <summary>
    /// The reason shown against the movement: a Waste shows its <see cref="WasteReason"/> and any
    /// note (<c>Spoiled — weevils</c>); every other type shows its free-text reason.
    /// </summary>
    public static string ReasonText(StockMovement movement)
    {
        if (movement.Type == MovementType.Wasted)
        {
            var reason = movement.WasteReason?.ToString() ?? "";
            return string.IsNullOrWhiteSpace(movement.Note) ? reason : $"{reason} — {movement.Note}";
        }

        return movement.Reason ?? "";
    }

    /// <summary>
    /// Movements newest-first for the audit view: most recent timestamp first, with id descending
    /// as a stable tiebreaker so movements sharing a timestamp (e.g. seeded rows posted off one
    /// clock) still read latest-first.
    /// </summary>
    public static IReadOnlyList<StockMovement> NewestFirst(IEnumerable<StockMovement> movements)
        => movements
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.Id)
            .ToList();
}
