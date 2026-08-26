using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;

namespace RestaurantInventory.Tests.Integration;

/// <summary>
/// The ledger behaviours exercised through EF Core against a real (in-memory) SQLite database:
/// posting persists and reloads correctly, the cached quantity survives a round-trip, movements
/// are insert-only, and the Shortage/Waste queries return the right answers end to end.
/// </summary>
public sealed class InventoryServiceSqliteTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteInMemoryDatabase _db = new();
    private readonly TestClock _clock = new(Now);

    private InventoryService NewService() => new(_db.NewContext(), _clock);

    private int SeedItem(string name = "Flour", decimal unitCost = 2m, decimal reorderLevel = 5m)
    {
        using var context = _db.NewContext();
        var item = new InventoryItem(name, UnitOfMeasure.Kg, unitCost, reorderLevel);
        context.InventoryItems.Add(item);
        context.SaveChanges();
        return item.Id;
    }

    [Fact]
    public async Task PostMovement_PersistsCachedQuantity_AcrossContexts()
    {
        var itemId = SeedItem();

        await NewService().PostMovementAsync(itemId, MovementType.Received, 20m);
        await NewService().PostMovementAsync(itemId, MovementType.Wasted, 3m, wasteReason: WasteReason.Spoiled);

        // Reload through a brand-new context: the cache must have been written to the DB.
        await using var verify = _db.NewContext();
        var reloaded = await verify.InventoryItems.SingleAsync(i => i.Id == itemId);
        Assert.Equal(17m, reloaded.QuantityOnHand);
    }

    [Fact]
    public async Task Movements_AreInsertOnly()
    {
        var itemId = SeedItem();

        var received = await NewService().PostMovementAsync(itemId, MovementType.Received, 10m);
        // A compensating adjustment must add a row, never rewrite the earlier one.
        await NewService().PostMovementAsync(itemId, MovementType.Adjusted, -4m, reason: "recount");

        await using var verify = _db.NewContext();
        var movements = await verify.StockMovements
            .Where(m => m.InventoryItemId == itemId)
            .OrderBy(m => m.Id)
            .ToListAsync();

        Assert.Equal(2, movements.Count);
        // The original Received row is untouched.
        var original = movements.Single(m => m.Id == received.Id);
        Assert.Equal(MovementType.Received, original.Type);
        Assert.Equal(10m, original.Quantity);
    }

    [Fact]
    public async Task PostMovement_StampsTimestampInUtc()
    {
        var itemId = SeedItem();
        await NewService().PostMovementAsync(itemId, MovementType.Received, 5m);

        await using var verify = _db.NewContext();
        var movement = await verify.StockMovements.SingleAsync(m => m.InventoryItemId == itemId);
        Assert.Equal(TimeSpan.Zero, movement.Timestamp.Offset);
        Assert.Equal(Now, movement.Timestamp);
    }

    [Fact]
    public async Task InvalidMovement_IsRejected_AndNothingPersists()
    {
        var itemId = SeedItem();

        await Assert.ThrowsAsync<InvalidMovementException>(
            () => NewService().PostMovementAsync(itemId, MovementType.Wasted, 3m, wasteReason: null));

        await using var verify = _db.NewContext();
        Assert.Empty(await verify.StockMovements.ToListAsync());
        var item = await verify.InventoryItems.SingleAsync(i => i.Id == itemId);
        Assert.Equal(0m, item.QuantityOnHand);
    }

    [Fact]
    public async Task PostMovement_ToUnknownItem_Throws()
    {
        await Assert.ThrowsAsync<InvalidMovementException>(
            () => NewService().PostMovementAsync(9999, MovementType.Received, 1m));
    }

    [Fact]
    public async Task GetShortages_ReturnsItemsAtOrBelowReorderLevel_ComputedNotStored()
    {
        var shortId = SeedItem("Salt", unitCost: 1m, reorderLevel: 5m);
        var boundaryId = SeedItem("Sugar", unitCost: 1m, reorderLevel: 5m);
        var healthyId = SeedItem("Rice", unitCost: 1m, reorderLevel: 5m);

        await NewService().PostMovementAsync(shortId, MovementType.Received, 2m);     // below
        await NewService().PostMovementAsync(boundaryId, MovementType.Received, 5m);  // exactly at level
        await NewService().PostMovementAsync(healthyId, MovementType.Received, 9m);   // above

        var shortages = await NewService().GetShortagesAsync();

        var ids = shortages.Select(i => i.Id).ToList();
        Assert.Contains(shortId, ids);
        Assert.Contains(boundaryId, ids);
        Assert.DoesNotContain(healthyId, ids);
    }

    [Fact]
    public async Task GetWasteByReason_TotalsMoney_WithinWindow_ThroughEfCore()
    {
        var itemId = SeedItem("Tomatoes", unitCost: 3m, reorderLevel: 0m);
        await NewService().PostMovementAsync(itemId, MovementType.Received, 100m);

        // Two wastes now, inside the default window.
        await NewService().PostMovementAsync(itemId, MovementType.Wasted, 4m, wasteReason: WasteReason.Spoiled);
        await NewService().PostMovementAsync(itemId, MovementType.Wasted, 1m, wasteReason: WasteReason.Expired);

        // One waste well before the window.
        _clock.Set(Now - TimeSpan.FromDays(60));
        await NewService().PostMovementAsync(itemId, MovementType.Wasted, 10m, wasteReason: WasteReason.Spoiled);
        _clock.Set(Now);

        var breakdown = await NewService().GetWasteByReasonAsync(); // default 30-day window

        // The 60-day-old spoilage is excluded: Spoiled = 4 * 3 = 12, Expired = 1 * 3 = 3.
        var spoiled = breakdown.Single(w => w.Reason == WasteReason.Spoiled);
        var expired = breakdown.Single(w => w.Reason == WasteReason.Expired);
        Assert.Equal(12m, spoiled.MoneyValue);
        Assert.Equal(3m, expired.MoneyValue);
        // Costliest first.
        Assert.Equal(WasteReason.Spoiled, breakdown[0].Reason);
    }

    public void Dispose() => _db.Dispose();
}
