using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Persistence;

namespace RestaurantInventory.Core.Services;

/// <summary>
/// The single application seam for all ledger behaviour (per the spec's primary seam):
/// posting movements, and answering which items are in Shortage and what Waste is costing.
/// Blazor pages are thin callers of this. Invariants live on the <see cref="InventoryItem"/>
/// aggregate; this service adds persistence, the UTC clock, and the read queries.
/// </summary>
/// <remarks>
/// Shortage and Waste are evaluated in memory after loading, not translated to SQL. SQLite
/// has no native decimal, so server-side decimal comparison and SUM are unreliable; for a
/// single-restaurant MVP the row counts are tiny, so correctness beats pushing the maths down.
/// </remarks>
public sealed class InventoryService
{
    /// <summary>How far back the Waste breakdown looks unless a caller asks for another window.</summary>
    public static readonly TimeSpan DefaultWasteWindow = TimeSpan.FromDays(30);

    private readonly InventoryDbContext _db;
    private readonly TimeProvider _clock;

    public InventoryService(InventoryDbContext db, TimeProvider? clock = null)
    {
        _db = db;
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>
    /// Posts a movement for one item: validates it, appends it to the ledger, and updates
    /// the cached <see cref="InventoryItem.QuantityOnHand"/> — all in a single save. The
    /// timestamp is stamped in UTC here so persisted ordering matches chronological order.
    /// </summary>
    /// <exception cref="InvalidMovementException">
    /// The item does not exist, or the movement breaks a ledger invariant. Nothing is persisted.
    /// </exception>
    public async Task<StockMovement> PostMovementAsync(
        int inventoryItemId,
        MovementType type,
        decimal quantity,
        string? reason = null,
        WasteReason? wasteReason = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        // Only the item row and its cached quantity are needed: PostMovement appends a new
        // movement (EF inserts it via the tracked navigation) and updates the cached scalar.
        // Loading the full movement history here would be unbounded work the write never reads.
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken)
            ?? throw new InvalidMovementException($"InventoryItem {inventoryItemId} does not exist.");

        var movement = item.PostMovement(type, quantity, _clock.GetUtcNow(), reason, wasteReason, note);
        await _db.SaveChangesAsync(cancellationToken);
        return movement;
    }

    /// <summary>
    /// Items currently in Shortage (<see cref="InventoryItem.IsInShortage"/>), name-ordered.
    /// </summary>
    public async Task<IReadOnlyList<InventoryItem>> GetShortagesAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.InventoryItems.AsNoTracking().ToListAsync(cancellationToken);
        return items
            .Where(i => i.IsInShortage)
            .OrderBy(i => i.Name)
            .ToList();
    }

    /// <summary>
    /// Waste totalled by <see cref="WasteReason"/> and valued in money over a recent window
    /// (default <see cref="DefaultWasteWindow"/>), costliest reason first.
    /// </summary>
    public async Task<IReadOnlyList<WasteByReason>> GetWasteByReasonAsync(
        TimeSpan? window = null,
        CancellationToken cancellationToken = default)
    {
        var cutoff = _clock.GetUtcNow() - (window ?? DefaultWasteWindow);

        var wasted = await _db.StockMovements
            .AsNoTracking()
            .Include(m => m.InventoryItem)
            .Where(m => m.Type == MovementType.Wasted)
            .ToListAsync(cancellationToken);

        return SummariseWaste(wasted, cutoff);
    }

    /// <summary>
    /// Pure aggregation: group Wasted movements at or after <paramref name="cutoff"/> by reason,
    /// summing quantity and money value (<c>quantity × UnitCost</c>, rounded to two places).
    /// Kept static and side-effect-free so the waste maths and the window boundary are unit-testable
    /// without a database.
    /// </summary>
    public static IReadOnlyList<WasteByReason> SummariseWaste(
        IEnumerable<StockMovement> wastedMovements,
        DateTimeOffset cutoff)
    {
        return wastedMovements
            .Where(m => m.Type == MovementType.Wasted && m.Timestamp >= cutoff)
            .GroupBy(m => m.WasteReason!.Value)
            .Select(g => new WasteByReason(
                g.Key,
                g.Sum(m => m.Quantity),
                decimal.Round(
                    g.Sum(m => m.Quantity * (m.InventoryItem?.UnitCost ?? 0m)),
                    2,
                    MidpointRounding.AwayFromZero)))
            .OrderByDescending(w => w.MoneyValue)
            .ThenBy(w => w.Reason)
            .ToList();
    }
}
