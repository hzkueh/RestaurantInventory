using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;

namespace RestaurantInventory.Tests.Integration;

/// <summary>
/// The recent-movements query backing the AI briefing (ticket 08), exercised through EF Core
/// against real in-memory SQLite. This is the query's proving ground: SQLite cannot translate an
/// ORDER BY over a <see cref="StockMovement.Timestamp"/> (DateTimeOffset), so the ordering must
/// happen in memory — a fact only a real-provider test can confirm.
/// </summary>
public sealed class RecentMovementsSqliteTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteInMemoryDatabase _db = new();
    private readonly TestClock _clock = new(Start);

    private InventoryService NewService() => new(_db.NewContext(), _clock);

    private int SeedItem(string name)
    {
        using var context = _db.NewContext();
        var item = new InventoryItem(name, UnitOfMeasure.Kg, unitCost: 2m, reorderLevel: 0m);
        context.InventoryItems.Add(item);
        context.SaveChanges();
        return item.Id;
    }

    [Fact]
    public async Task GetRecentMovements_ReturnsMovementsNewestFirst_AcrossItems()
    {
        var flour = SeedItem("Flour");
        var rice = SeedItem("Rice");

        _clock.Set(Start);
        await NewService().PostMovementAsync(flour, MovementType.Received, 10m);
        _clock.Set(Start.AddHours(1));
        await NewService().PostMovementAsync(rice, MovementType.Received, 20m);
        _clock.Set(Start.AddHours(2));
        var newest = await NewService().PostMovementAsync(flour, MovementType.Wasted, 1m, wasteReason: WasteReason.Spoiled);

        var recent = await NewService().GetRecentMovementsAsync();

        Assert.Equal(3, recent.Count);
        Assert.Equal(newest.Id, recent[0].Id); // most recent timestamp first
        Assert.Equal("Flour", recent[0].InventoryItem!.Name); // navigation is loaded for the narrative
    }

    [Fact]
    public async Task GetRecentMovements_CapsResultsAtTheLimit()
    {
        var itemId = SeedItem("Oil");
        for (var i = 0; i < 5; i++)
        {
            _clock.Set(Start.AddMinutes(i));
            await NewService().PostMovementAsync(itemId, MovementType.Received, 1m);
        }

        var recent = await NewService().GetRecentMovementsAsync(limit: 3);

        Assert.Equal(3, recent.Count);
    }

    public void Dispose() => _db.Dispose();
}
