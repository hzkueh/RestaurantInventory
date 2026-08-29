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
    /// Creates a new <see cref="InventoryItem"/> with a fixed <see cref="UnitOfMeasure"/> and the
    /// manager-set metadata, and persists it so it appears in the items list (user stories 5–6).
    /// </summary>
    /// <exception cref="InvalidItemException">The metadata is invalid; nothing is persisted.</exception>
    public async Task<InventoryItem> CreateItemAsync(
        string name,
        UnitOfMeasure unitOfMeasure,
        decimal unitCost,
        decimal reorderLevel,
        CancellationToken cancellationToken = default)
    {
        var item = new InventoryItem(name, unitOfMeasure, unitCost, reorderLevel);
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <summary>
    /// Updates an existing item's editable metadata — name, <see cref="InventoryItem.UnitCost"/>,
    /// and <see cref="InventoryItem.ReorderLevel"/> (user story 7). The item's
    /// <see cref="UnitOfMeasure"/> is intentionally not a parameter: it is fixed after creation
    /// (user story 8), so there is no way to change it through this seam.
    /// </summary>
    /// <exception cref="InvalidItemException">
    /// No item has that id, or the new metadata is invalid; nothing is persisted.
    /// </exception>
    public async Task UpdateItemAsync(
        int id,
        string name,
        decimal unitCost,
        decimal reorderLevel,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidItemException($"InventoryItem {id} does not exist.");

        item.UpdateDetails(name, unitCost, reorderLevel);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Every item name-ordered, or — when <paramref name="shortagesOnly"/> is set — only those
    /// currently in Shortage (<see cref="InventoryItem.IsInShortage"/>). Backs the items list and
    /// its Shortage filter; the returned items carry their cached <see cref="InventoryItem.QuantityOnHand"/>
    /// and computed Shortage flag so a caller can render the badge without further queries.
    /// </summary>
    public async Task<IReadOnlyList<InventoryItem>> GetItemsAsync(
        bool shortagesOnly = false,
        CancellationToken cancellationToken = default)
    {
        var items = await _db.InventoryItems.AsNoTracking().ToListAsync(cancellationToken);
        IEnumerable<InventoryItem> result = items;
        if (shortagesOnly)
        {
            result = result.Where(i => i.IsInShortage);
        }

        return result.OrderBy(i => i.Name).ToList();
    }

    /// <summary>
    /// Items currently in Shortage (<see cref="InventoryItem.IsInShortage"/>), name-ordered.
    /// The single Shortage query — the list filter and the dashboard both go through it.
    /// </summary>
    public Task<IReadOnlyList<InventoryItem>> GetShortagesAsync(CancellationToken cancellationToken = default)
        => GetItemsAsync(shortagesOnly: true, cancellationToken);

    /// <summary>
    /// One item together with its full StockMovement history for the detail page, or
    /// <c>null</c> when no item has that id. Movements are loaded but not ordered here —
    /// the caller sorts for display (newest-first).
    /// </summary>
    public async Task<InventoryItem?> GetItemDetailAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.InventoryItems
            .AsNoTracking()
            .Include(i => i.Movements)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    /// <summary>
    /// The most recent StockMovements across all items, newest-first, capped at
    /// <paramref name="limit"/>. Backs the AI briefing's "recent movements" section (ticket 08);
    /// each movement carries its <see cref="StockMovement.InventoryItem"/> so a caller can name the
    /// item and its unit without further queries.
    /// </summary>
    public async Task<IReadOnlyList<StockMovement>> GetRecentMovementsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _db.StockMovements
            .AsNoTracking()
            .Include(m => m.InventoryItem)
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
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
