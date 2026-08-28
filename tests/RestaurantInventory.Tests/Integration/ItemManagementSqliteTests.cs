using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;

namespace RestaurantInventory.Tests.Integration;

/// <summary>
/// The create/edit item behaviour on the inventory service seam (ticket 05), exercised through EF
/// Core against a real in-memory SQLite database: a created item is persisted and appears in the
/// list; an edit changes only name/unit cost/reorder level and leaves the fixed UnitOfMeasure and
/// the cached QuantityOnHand untouched; editing a missing item is rejected.
/// </summary>
public sealed class ItemManagementSqliteTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteInMemoryDatabase _db = new();
    private readonly TestClock _clock = new(Now);

    private InventoryService NewService() => new(_db.NewContext(), _clock);

    [Fact]
    public async Task CreateItem_PersistsAndAppearsInList()
    {
        var created = await NewService().CreateItemAsync("Basmati rice", UnitOfMeasure.Kg, unitCost: 1.80m, reorderLevel: 10m);

        Assert.True(created.Id > 0);

        var items = await NewService().GetItemsAsync();
        var rice = Assert.Single(items);
        Assert.Equal("Basmati rice", rice.Name);
        Assert.Equal(UnitOfMeasure.Kg, rice.UnitOfMeasure);
        Assert.Equal(1.80m, rice.UnitCost);
        Assert.Equal(10m, rice.ReorderLevel);
        Assert.Equal(0m, rice.QuantityOnHand);
    }

    [Fact]
    public async Task UpdateItem_ChangesEditableFields_AndPersists()
    {
        var created = await NewService().CreateItemAsync("Rice", UnitOfMeasure.Kg, 1.80m, 10m);

        await NewService().UpdateItemAsync(created.Id, "Basmati rice", unitCost: 2.10m, reorderLevel: 15m);

        var reloaded = await NewService().GetItemDetailAsync(created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Basmati rice", reloaded!.Name);
        Assert.Equal(2.10m, reloaded.UnitCost);
        Assert.Equal(15m, reloaded.ReorderLevel);
    }

    [Fact]
    public async Task UpdateItem_LeavesUnitOfMeasureAndQuantityUntouched()
    {
        var created = await NewService().CreateItemAsync("Rice", UnitOfMeasure.Kg, 1.80m, 10m);
        await NewService().PostMovementAsync(created.Id, MovementType.Received, 25m);

        await NewService().UpdateItemAsync(created.Id, "Basmati rice", 2.10m, 15m);

        var reloaded = await NewService().GetItemDetailAsync(created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(UnitOfMeasure.Kg, reloaded!.UnitOfMeasure);
        Assert.Equal(25m, reloaded.QuantityOnHand);
    }

    [Fact]
    public async Task UpdateItem_ForUnknownId_Throws()
    {
        await Assert.ThrowsAsync<InvalidItemException>(
            () => NewService().UpdateItemAsync(9999, "Ghost", 1m, 1m));
    }

    [Fact]
    public async Task CreateItem_RejectsInvalidInput_AndPersistsNothing()
    {
        await Assert.ThrowsAsync<InvalidItemException>(
            () => NewService().CreateItemAsync("  ", UnitOfMeasure.Kg, 1m, 1m));

        var items = await NewService().GetItemsAsync();
        Assert.Empty(items);
    }

    public void Dispose() => _db.Dispose();
}
