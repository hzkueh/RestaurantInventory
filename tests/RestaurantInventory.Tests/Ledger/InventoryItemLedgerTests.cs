using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Tests.Ledger;

/// <summary>
/// Fast, pure tests of the ledger maths on the <see cref="InventoryItem"/> aggregate —
/// no database. They assert externally observable outcomes (resulting quantity, shortage,
/// what is/is not appended), never private fields.
/// </summary>
public class InventoryItemLedgerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    private static InventoryItem NewItem(decimal reorderLevel = 5m, decimal unitCost = 2m) =>
        new("Flour", UnitOfMeasure.Kg, unitCost, reorderLevel);

    [Fact]
    public void SequenceOfMovements_ResolvesToExpectedQuantity()
    {
        var item = NewItem();

        item.PostMovement(MovementType.Received, 20m, T0);
        item.PostMovement(MovementType.Wasted, 3m, T0, wasteReason: WasteReason.Spoiled);
        item.PostMovement(MovementType.Adjusted, -2m, T0, reason: "stock count");

        // 0 + 20 - 3 - 2
        Assert.Equal(15m, item.QuantityOnHand);
        Assert.Equal(3, item.Movements.Count);
    }

    [Fact]
    public void CompensatingMovement_RestoresQuantity_WithoutEditingHistory()
    {
        var item = NewItem();
        item.PostMovement(MovementType.Received, 10m, T0);

        // A waste posted in error...
        var mistake = item.PostMovement(MovementType.Wasted, 4m, T0, wasteReason: WasteReason.Spilled);
        Assert.Equal(6m, item.QuantityOnHand);

        // ...corrected by a compensating positive adjustment, not by deleting the waste.
        item.PostMovement(MovementType.Adjusted, 4m, T0, reason: "reverse mistaken waste");

        Assert.Equal(10m, item.QuantityOnHand);
        // History is append-only: the erroneous movement is still there, unchanged.
        Assert.Equal(3, item.Movements.Count);
        Assert.Contains(mistake, item.Movements);
        Assert.Equal(4m, mistake.Quantity);
        Assert.Equal(MovementType.Wasted, mistake.Type);
    }

    [Fact]
    public void Adjusted_AcceptsNegativeQuantity()
    {
        var item = NewItem();
        item.PostMovement(MovementType.Received, 8m, T0);

        item.PostMovement(MovementType.Adjusted, -5m, T0, reason: "spillage recount");

        Assert.Equal(3m, item.QuantityOnHand);
    }

    [Theory]
    [InlineData(5.0)]   // exactly at the reorder level -> in shortage (inclusive boundary)
    [InlineData(4.999)] // below -> in shortage
    public void IsInShortage_True_AtOrBelowReorderLevel(double quantity)
    {
        var item = NewItem(reorderLevel: 5m);
        item.PostMovement(MovementType.Received, (decimal)quantity, T0);

        Assert.True(item.IsInShortage);
    }

    [Fact]
    public void IsInShortage_False_JustAboveReorderLevel()
    {
        var item = NewItem(reorderLevel: 5m);
        item.PostMovement(MovementType.Received, 5.001m, T0);

        Assert.False(item.IsInShortage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Received_RejectsNonPositiveQuantity(int quantity)
    {
        var item = NewItem();

        Assert.Throws<InvalidMovementException>(
            () => item.PostMovement(MovementType.Received, quantity, T0));

        AssertNothingHappened(item);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Wasted_RejectsNonPositiveQuantity(int quantity)
    {
        var item = NewItem();

        Assert.Throws<InvalidMovementException>(
            () => item.PostMovement(MovementType.Wasted, quantity, T0, wasteReason: WasteReason.Spoiled));

        AssertNothingHappened(item);
    }

    [Fact]
    public void Wasted_RequiresWasteReason()
    {
        var item = NewItem();

        Assert.Throws<InvalidMovementException>(
            () => item.PostMovement(MovementType.Wasted, 3m, T0, wasteReason: null));

        AssertNothingHappened(item);
    }

    [Fact]
    public void Adjusted_RejectsZeroQuantity()
    {
        var item = NewItem();

        Assert.Throws<InvalidMovementException>(
            () => item.PostMovement(MovementType.Adjusted, 0m, T0, reason: "no-op"));

        AssertNothingHappened(item);
    }

    [Fact]
    public void Adjusted_RequiresReason()
    {
        var item = NewItem();

        Assert.Throws<InvalidMovementException>(
            () => item.PostMovement(MovementType.Adjusted, -2m, T0, reason: "  "));

        AssertNothingHappened(item);
    }

    [Fact]
    public void RejectedMovement_IsNotAppended_AndCacheIsUntouched()
    {
        var item = NewItem();
        item.PostMovement(MovementType.Received, 10m, T0);

        Assert.Throws<InvalidMovementException>(
            () => item.PostMovement(MovementType.Received, -1m, T0));

        Assert.Equal(10m, item.QuantityOnHand);
        Assert.Single(item.Movements);
    }

    private static void AssertNothingHappened(InventoryItem item)
    {
        Assert.Equal(0m, item.QuantityOnHand);
        Assert.Empty(item.Movements);
    }
}
