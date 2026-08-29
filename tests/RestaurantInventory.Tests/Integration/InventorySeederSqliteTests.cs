using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Persistence;
using RestaurantInventory.Core.Services;

namespace RestaurantInventory.Tests.Integration;

/// <summary>
/// First-run seeding (user story 30), proven through EF Core against real in-memory SQLite —
/// the same provider the app runs on. These tests assert the demonstrable outcomes a reviewer
/// would see on a fresh clone (populated screens, some Shortages, a non-empty Waste breakdown),
/// and the two invariants that make the seed trustworthy: the cached
/// <see cref="InventoryItem.QuantityOnHand"/> matches the ledger, and re-running is a no-op.
/// They deliberately do not assert exact seed values, so the fixture can be tuned freely.
/// </summary>
public sealed class InventorySeederSqliteTests : IDisposable
{
    // Seeding backdates Waste relative to "now"; a fixed clock keeps the recent-window
    // assertions deterministic. The clock the seeder uses is the clock the dashboard uses.
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteInMemoryDatabase _db = new();
    private readonly TestClock _clock = new(Now);

    /// <summary>The signed effect a movement has on stock — the ledger's own definition, recomputed here.</summary>
    private static decimal SignedDelta(StockMovement m) => m.Type switch
    {
        MovementType.Received => m.Quantity,
        MovementType.Wasted => -m.Quantity,
        MovementType.Adjusted => m.Quantity,
        _ => throw new InvalidOperationException($"Unknown movement type '{m.Type}'."),
    };

    [Fact]
    public async Task SeedAsync_PopulatesItemsWithVariedMetadataAndMovements()
    {
        using (var context = _db.NewContext())
        {
            await InventorySeeder.SeedAsync(context, _clock);
        }

        using var read = _db.NewContext();
        var items = read.InventoryItems.ToList();

        Assert.NotEmpty(items);
        // Varied UnitOfMeasure / UnitCost / ReorderLevel — a demonstrable, not uniform, catalogue.
        Assert.True(items.Select(i => i.UnitOfMeasure).Distinct().Count() >= 3);
        Assert.True(items.Select(i => i.UnitCost).Distinct().Count() >= 3);
        Assert.True(items.Select(i => i.ReorderLevel).Distinct().Count() >= 3);
        Assert.Contains(read.StockMovements, _ => true); // history exists to back the detail pages
    }

    [Fact]
    public async Task SeedAsync_LeavesCachedQuantityMatchingTheLedger()
    {
        using (var context = _db.NewContext())
        {
            await InventorySeeder.SeedAsync(context, _clock);
        }

        // Reload in a fresh context so we are reading persisted state, not a tracker's cache.
        using var read = _db.NewContext();
        var items = read.InventoryItems.Include(i => i.Movements).ToList();

        Assert.NotEmpty(items);
        foreach (var item in items)
        {
            var ledger = item.Movements.Sum(SignedDelta);
            Assert.Equal(ledger, item.QuantityOnHand);
        }
    }

    [Fact]
    public async Task SeedAsync_LandsSomeItemsInShortage()
    {
        using (var context = _db.NewContext())
        {
            await InventorySeeder.SeedAsync(context, _clock);
        }

        var shortages = await new InventoryService(_db.NewContext(), _clock).GetShortagesAsync();

        Assert.NotEmpty(shortages);
    }

    [Fact]
    public async Task SeedAsync_ProducesANonEmptyRecentWasteBreakdown()
    {
        using (var context = _db.NewContext())
        {
            await InventorySeeder.SeedAsync(context, _clock);
        }

        // The dashboard's default 30-day window, evaluated at the same "now" the seed backdated to.
        var waste = await new InventoryService(_db.NewContext(), _clock).GetWasteByReasonAsync();

        Assert.NotEmpty(waste);
        Assert.True(waste.Sum(w => w.MoneyValue) > 0m);
        // Multiple reasons so the breakdown chart is worth showing, not a single bar.
        Assert.True(waste.Select(w => w.Reason).Distinct().Count() >= 2);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_NotDuplicatingOnSecondRun()
    {
        using (var first = _db.NewContext())
        {
            await InventorySeeder.SeedAsync(first, _clock);
        }

        int itemsAfterFirst, movementsAfterFirst;
        using (var count = _db.NewContext())
        {
            itemsAfterFirst = count.InventoryItems.Count();
            movementsAfterFirst = count.StockMovements.Count();
        }

        using (var second = _db.NewContext())
        {
            await InventorySeeder.SeedAsync(second, _clock);
        }

        using var read = _db.NewContext();
        Assert.Equal(itemsAfterFirst, read.InventoryItems.Count());
        Assert.Equal(movementsAfterFirst, read.StockMovements.Count());
    }

    public void Dispose() => _db.Dispose();
}
