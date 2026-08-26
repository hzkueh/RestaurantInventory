using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Tests;

/// <summary>
/// Minimal scaffold sanity checks. The real ledger behaviour is covered by the
/// inventory-service tests in ticket 02; these just assert the domain spine is wired.
/// </summary>
public class ScaffoldSmokeTests
{
    [Fact]
    public void NewItem_StartsWithZeroQuantityOnHand()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, unitCost: 1.20m, reorderLevel: 5m);

        Assert.Equal(0m, item.QuantityOnHand);
    }

    [Fact]
    public void NewItem_PreservesUnitOfMeasure()
    {
        var item = new InventoryItem("Olive oil", UnitOfMeasure.L, unitCost: 8m, reorderLevel: 2m);

        Assert.Equal(UnitOfMeasure.L, item.UnitOfMeasure);
    }
}
