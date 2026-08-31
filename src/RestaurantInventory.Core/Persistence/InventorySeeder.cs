using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Core.Persistence;

/// <summary>
/// Seeds a realistic starting catalogue on first run (user story 30): varied InventoryItems and
/// a history of StockMovements, so the items list (with Shortages), item detail, dashboard, and
/// the AI summary are all demonstrable with no manual setup. Idempotent — it does nothing once
/// any item exists — so clone-and-run and every restart after leave the data untouched.
/// </summary>
/// <remarks>
/// Movements are posted through the <see cref="InventoryItem"/> aggregate, so the cached
/// <see cref="InventoryItem.QuantityOnHand"/> is maintained by the same code the live app uses and
/// can never diverge from the ledger. Waste is backdated to the last few days — inside the
/// dashboard's recent window — so its money-by-reason breakdown is non-empty at first sight, and
/// timestamps are derived from the injected clock rather than <c>DateTimeOffset.Now</c> so the
/// window stays honest under a test clock.
/// </remarks>
public static class InventorySeeder
{
    /// <summary>
    /// Populates <paramref name="db"/> with the demo catalogue if — and only if — it holds no
    /// InventoryItems yet, saving items and their movements in one transaction. Returns without
    /// touching the database when data is already present.
    /// </summary>
    public static async Task SeedAsync(
        InventoryDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        if (await db.InventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow();
        db.InventoryItems.AddRange(BuildCatalogue(now));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>A single seeded movement, its timestamp expressed as whole days before "now".</summary>
    private sealed record Move(
        MovementType Type,
        decimal Quantity,
        int DaysAgo,
        string? Reason = null,
        WasteReason? WasteReason = null,
        string? Note = null);

    /// <summary>
    /// The realistic kitchen catalogue. Costs, units, and reorder levels are deliberately varied;
    /// several items are arranged to finish at or below their reorder level so the Shortage badge
    /// and filter have something to show, and Waste spans every WasteReason within the recent
    /// window so the dashboard breakdown is populated across reasons.
    /// </summary>
    private static IEnumerable<InventoryItem> BuildCatalogue(DateTimeOffset now)
    {
        // Not in Shortage: a healthy staple with a spoilage write-off. 25 - 2 = 23 kg (reorder 10).
        yield return Build(now, "All-Purpose Flour", UnitOfMeasure.Kg, unitCost: 1.20m, reorderLevel: 10m,
            new Move(MovementType.Received, 25m, DaysAgo: 21),
            new Move(MovementType.Wasted, 2m, DaysAgo: 4, WasteReason: WasteReason.Spoiled, Note: "Damp sack in dry store"));

        // Not in Shortage: delivery, a spill, and a physical-count correction. 20 - 0.5 - 1 = 18.5 kg (reorder 8).
        yield return Build(now, "Basmati Rice", UnitOfMeasure.Kg, unitCost: 2.50m, reorderLevel: 8m,
            new Move(MovementType.Received, 20m, DaysAgo: 18),
            new Move(MovementType.Wasted, 0.5m, DaysAgo: 3, WasteReason: WasteReason.Spilled),
            new Move(MovementType.Adjusted, -1m, DaysAgo: 1, Reason: "Physical count correction"));

        // Not in Shortage: a costlier liquid measured in litres. 10 - 0.5 = 9.5 L (reorder 5).
        yield return Build(now, "Extra Virgin Olive Oil", UnitOfMeasure.L, unitCost: 8.00m, reorderLevel: 5m,
            new Move(MovementType.Received, 10m, DaysAgo: 25),
            new Move(MovementType.Wasted, 0.5m, DaysAgo: 6, WasteReason: WasteReason.Spilled, Note: "Bottle knocked over on the line"));

        // In Shortage: perishable that expired heavily. 15 - 4 = 11 L, at/below reorder 12.
        yield return Build(now, "Whole Milk", UnitOfMeasure.L, unitCost: 1.10m, reorderLevel: 12m,
            new Move(MovementType.Received, 15m, DaysAgo: 9),
            new Move(MovementType.Wasted, 4m, DaysAgo: 1, WasteReason: WasteReason.Expired, Note: "Past date on Monday delivery"));

        // Not in Shortage: two waste reasons on one perishable. 12 - 3 - 2 = 7 kg (reorder 6).
        yield return Build(now, "Fresh Tomatoes", UnitOfMeasure.Kg, unitCost: 3.20m, reorderLevel: 6m,
            new Move(MovementType.Received, 12m, DaysAgo: 8),
            new Move(MovementType.Wasted, 3m, DaysAgo: 2, WasteReason: WasteReason.Spoiled),
            new Move(MovementType.Wasted, 2m, DaysAgo: 5, WasteReason: WasteReason.Overproduction, Note: "Prepped too much for a slow service"));

        // Not in Shortage: high-value protein with a small trim loss. 15 - 1 - 1 = 13 kg (reorder 10).
        yield return Build(now, "Chicken Breast", UnitOfMeasure.Kg, unitCost: 6.50m, reorderLevel: 10m,
            new Move(MovementType.Received, 15m, DaysAgo: 7),
            new Move(MovementType.Wasted, 1m, DaysAgo: 2, WasteReason: WasteReason.Spoiled, Note: "Left out of the walk-in overnight"),
            new Move(MovementType.Adjusted, -1m, DaysAgo: 1, Reason: "Reconciled against physical count"));

        // In Shortage at the exact boundary: 90 - 30 = 60 each, equal to reorder 60 (inclusive).
        yield return Build(now, "Eggs", UnitOfMeasure.Each, unitCost: 0.25m, reorderLevel: 60m,
            new Move(MovementType.Received, 90m, DaysAgo: 12),
            new Move(MovementType.Wasted, 30m, DaysAgo: 2, WasteReason: WasteReason.Expired, Note: "Two trays past date"));

        // Not in Shortage: a cheap dry good bought in bulk, no waste. 20 kg (reorder 5). Salt is
        // priced per kilo rather than per gram so its UnitCost survives the 2-dp UnitCost column
        // (a sub-cent per-gram cost would round to 0.00); Saffron keeps a grams item in the mix.
        yield return Build(now, "Table Salt", UnitOfMeasure.Kg, unitCost: 0.80m, reorderLevel: 5m,
            new Move(MovementType.Received, 20m, DaysAgo: 28));

        // In Shortage: a very high-value spice where "Other" waste bites. 30 - 8 - 5 = 17 g (reorder 20).
        yield return Build(now, "Saffron", UnitOfMeasure.G, unitCost: 5.00m, reorderLevel: 20m,
            new Move(MovementType.Received, 30m, DaysAgo: 20),
            new Move(MovementType.Wasted, 8m, DaysAgo: 5, WasteReason: WasteReason.Other, Note: "Contaminated tin"),
            new Move(MovementType.Adjusted, -5m, DaysAgo: 2, Reason: "Physical count correction"));
    }

    /// <summary>
    /// Creates one item and replays its movements through the aggregate at their backdated
    /// timestamps, so the returned item already carries a correct cached
    /// <see cref="InventoryItem.QuantityOnHand"/> before EF Core ever sees it.
    /// </summary>
    private static InventoryItem Build(
        DateTimeOffset now,
        string name,
        UnitOfMeasure unitOfMeasure,
        decimal unitCost,
        decimal reorderLevel,
        params Move[] movements)
    {
        var item = new InventoryItem(name, unitOfMeasure, unitCost, reorderLevel);
        foreach (var move in movements)
        {
            item.PostMovement(
                move.Type,
                move.Quantity,
                now.AddDays(-move.DaysAgo),
                move.Reason,
                move.WasteReason,
                move.Note);
        }

        return item;
    }
}
