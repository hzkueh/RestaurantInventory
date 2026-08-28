using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;
using RestaurantInventory.Web.Components.Pages.Inventory;

namespace RestaurantInventory.Tests.Integration;

/// <summary>
/// The read queries the items list and item-detail screens (ticket 04) hang off: listing all
/// items, filtering to only those in Shortage, and loading one item with its movement history.
/// Exercised through EF Core against a real in-memory SQLite database so the queries are proven
/// end to end, not just against the in-memory tracker.
/// </summary>
public sealed class ItemQueriesSqliteTests : IDisposable
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
    public async Task GetItems_ReturnsEveryItem_NameOrdered()
    {
        SeedItem("Rice");
        SeedItem("Anchovies");
        SeedItem("Mustard");

        var items = await NewService().GetItemsAsync();

        Assert.Equal(new[] { "Anchovies", "Mustard", "Rice" }, items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public async Task GetItems_ReportsCachedQuantityAndShortageFlag_PerItem()
    {
        var shortId = SeedItem("Salt", reorderLevel: 5m);
        var healthyId = SeedItem("Sugar", reorderLevel: 5m);
        await NewService().PostMovementAsync(shortId, MovementType.Received, 5m);   // exactly at level → short
        await NewService().PostMovementAsync(healthyId, MovementType.Received, 9m);  // above level → healthy

        var items = await NewService().GetItemsAsync();

        var salt = items.Single(i => i.Id == shortId);
        var sugar = items.Single(i => i.Id == healthyId);
        Assert.Equal(5m, salt.QuantityOnHand);
        Assert.True(salt.IsInShortage);
        Assert.Equal(9m, sugar.QuantityOnHand);
        Assert.False(sugar.IsInShortage);
    }

    [Fact]
    public async Task GetItems_ShortagesOnly_ReturnsOnlyItemsInShortage()
    {
        var shortId = SeedItem("Salt", reorderLevel: 5m);
        var boundaryId = SeedItem("Sugar", reorderLevel: 5m);
        var healthyId = SeedItem("Rice", reorderLevel: 5m);
        await NewService().PostMovementAsync(shortId, MovementType.Received, 2m);      // below
        await NewService().PostMovementAsync(boundaryId, MovementType.Received, 5m);   // exactly at level
        await NewService().PostMovementAsync(healthyId, MovementType.Received, 9m);    // above

        var shortages = await NewService().GetItemsAsync(shortagesOnly: true);

        var ids = shortages.Select(i => i.Id).ToList();
        Assert.Contains(shortId, ids);
        Assert.Contains(boundaryId, ids);
        Assert.DoesNotContain(healthyId, ids);
    }

    [Fact]
    public async Task GetItemDetail_ReturnsItemWithAllItsMovements()
    {
        var itemId = SeedItem("Tomatoes", unitCost: 3m, reorderLevel: 4m);
        await NewService().PostMovementAsync(itemId, MovementType.Received, 20m);
        await NewService().PostMovementAsync(itemId, MovementType.Wasted, 2m, wasteReason: WasteReason.Spoiled, note: "back of walk-in");
        await NewService().PostMovementAsync(itemId, MovementType.Adjusted, -1m, reason: "recount");

        var item = await NewService().GetItemDetailAsync(itemId);

        Assert.NotNull(item);
        Assert.Equal("Tomatoes", item!.Name);
        Assert.Equal(17m, item.QuantityOnHand);
        Assert.Equal(3, item.Movements.Count);
        // The Wasted movement carried its reason and note through persistence.
        var wasted = item.Movements.Single(m => m.Type == MovementType.Wasted);
        Assert.Equal(WasteReason.Spoiled, wasted.WasteReason);
        Assert.Equal("back of walk-in", wasted.Note);
    }

    [Fact]
    public async Task GetItemDetail_ForUnknownId_ReturnsNull()
    {
        var item = await NewService().GetItemDetailAsync(9999);

        Assert.Null(item);
    }

    [Fact]
    public async Task MovementHistory_NewestFirst_BreaksTimestampTiesByIdDescending()
    {
        // The fixed TestClock stamps every movement with the same timestamp, so ordering falls
        // entirely to the id tiebreaker — the last posted must read first in the audit view.
        var itemId = SeedItem("Butter", reorderLevel: 0m);
        var first = await NewService().PostMovementAsync(itemId, MovementType.Received, 5m);
        var second = await NewService().PostMovementAsync(itemId, MovementType.Received, 5m);
        var third = await NewService().PostMovementAsync(itemId, MovementType.Wasted, 1m, wasteReason: WasteReason.Spoiled);

        var item = await NewService().GetItemDetailAsync(itemId);
        var ordered = MovementDisplay.NewestFirst(item!.Movements);

        Assert.Equal(new[] { third.Id, second.Id, first.Id }, ordered.Select(m => m.Id).ToArray());
    }

    public void Dispose() => _db.Dispose();
}
