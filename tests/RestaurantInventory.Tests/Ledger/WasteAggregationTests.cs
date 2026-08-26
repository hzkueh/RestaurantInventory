using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;

namespace RestaurantInventory.Tests.Ledger;

/// <summary>
/// Pure tests of <see cref="InventoryService.SummariseWaste"/>: money valuation by reason
/// and the recent-window boundary, with no database.
/// </summary>
public class WasteAggregationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a Wasted movement attached to an item of the given unit cost.</summary>
    private static StockMovement Waste(decimal unitCost, decimal quantity, WasteReason reason, DateTimeOffset at)
    {
        var item = new InventoryItem("Item", UnitOfMeasure.Kg, unitCost, reorderLevel: 0m);
        return item.PostMovement(MovementType.Wasted, quantity, at, wasteReason: reason);
    }

    [Fact]
    public void ValuesEachReasonInMoney_UsingItemUnitCost()
    {
        var movements = new[]
        {
            Waste(unitCost: 2.50m, quantity: 3m, WasteReason.Spoiled, Now),   // 7.50
            Waste(unitCost: 2.50m, quantity: 1m, WasteReason.Spoiled, Now),   // 2.50
            Waste(unitCost: 4.00m, quantity: 2m, WasteReason.Expired, Now),   // 8.00
        };

        var result = InventoryService.SummariseWaste(movements, cutoff: Now.AddDays(-30));

        Assert.Equal(2, result.Count);
        // Costliest reason first: Spoiled = 7.50 + 2.50 = 10.00, Expired = 8.00.
        Assert.Equal(WasteReason.Spoiled, result[0].Reason);
        Assert.Equal(10.00m, result[0].MoneyValue);
        Assert.Equal(4m, result[0].Quantity);

        Assert.Equal(WasteReason.Expired, result[1].Reason);
        Assert.Equal(8.00m, result[1].MoneyValue);
        Assert.Equal(2m, result[1].Quantity);
    }

    [Fact]
    public void MoneyValue_IsRoundedToTwoPlaces()
    {
        // 0.333 * 1.005 = 0.334665 -> 0.33
        var movements = new[] { Waste(unitCost: 1.005m, quantity: 0.333m, WasteReason.Other, Now) };

        var result = InventoryService.SummariseWaste(movements, cutoff: Now.AddDays(-30));

        Assert.Equal(0.33m, Assert.Single(result).MoneyValue);
    }

    [Fact]
    public void RecentWindow_IncludesBoundaryAndExcludesOlder()
    {
        var cutoff = Now.AddDays(-30);
        var movements = new[]
        {
            Waste(1m, 5m, WasteReason.Spoiled, cutoff),                       // exactly at cutoff -> included
            Waste(1m, 7m, WasteReason.Spoiled, cutoff.AddTicks(-1)),          // just before -> excluded
            Waste(1m, 2m, WasteReason.Spoiled, Now),                          // inside window -> included
        };

        var result = InventoryService.SummariseWaste(movements, cutoff);

        // Only the two at/after the cutoff count: 5 + 2 = 7.
        var spoiled = Assert.Single(result);
        Assert.Equal(WasteReason.Spoiled, spoiled.Reason);
        Assert.Equal(7m, spoiled.Quantity);
        Assert.Equal(7m, spoiled.MoneyValue);
    }

    [Fact]
    public void NoWasteInWindow_ReturnsEmpty()
    {
        var movements = new[] { Waste(1m, 5m, WasteReason.Spoiled, Now.AddDays(-60)) };

        var result = InventoryService.SummariseWaste(movements, cutoff: Now.AddDays(-30));

        Assert.Empty(result);
    }
}
