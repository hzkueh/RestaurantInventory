using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Tests.Domain;

/// <summary>
/// Fast, pure tests of the <see cref="InventoryItem"/> metadata invariants (ticket 05): the
/// three editable fields (name, unit cost, reorder level) and the rule that
/// <see cref="UnitOfMeasure"/> is fixed at creation so historical movements stay in one unit.
/// No database — the aggregate owns these invariants regardless of who calls it.
/// </summary>
public class InventoryItemTests
{
    [Fact]
    public void Constructor_SetsAllFields()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, unitCost: 1.20m, reorderLevel: 5m);

        Assert.Equal("Flour", item.Name);
        Assert.Equal(UnitOfMeasure.Kg, item.UnitOfMeasure);
        Assert.Equal(1.20m, item.UnitCost);
        Assert.Equal(5m, item.ReorderLevel);
        Assert.Equal(0m, item.QuantityOnHand);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var item = new InventoryItem("  Olive oil  ", UnitOfMeasure.L, 4m, 2m);

        Assert.Equal("Olive oil", item.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankName(string name)
    {
        Assert.Throws<InvalidItemException>(() => new InventoryItem(name, UnitOfMeasure.Kg, 1m, 1m));
    }

    [Fact]
    public void Constructor_RejectsNegativeUnitCost()
    {
        Assert.Throws<InvalidItemException>(() => new InventoryItem("Flour", UnitOfMeasure.Kg, -0.01m, 1m));
    }

    [Fact]
    public void Constructor_RejectsNegativeReorderLevel()
    {
        Assert.Throws<InvalidItemException>(() => new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, -1m));
    }

    [Fact]
    public void Constructor_RejectsNameLongerThanMax()
    {
        var tooLong = new string('x', InventoryItem.MaxNameLength + 1);

        Assert.Throws<InvalidItemException>(() => new InventoryItem(tooLong, UnitOfMeasure.Kg, 1m, 1m));
    }

    [Fact]
    public void UpdateDetails_AcceptsNameAtMaxLength()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);
        var atLimit = new string('x', InventoryItem.MaxNameLength);

        item.UpdateDetails(atLimit, 1m, 5m);

        Assert.Equal(atLimit, item.Name);
    }

    [Fact]
    public void UpdateDetails_ChangesEditableFields()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);

        item.UpdateDetails("Bread flour", unitCost: 1.35m, reorderLevel: 8m);

        Assert.Equal("Bread flour", item.Name);
        Assert.Equal(1.35m, item.UnitCost);
        Assert.Equal(8m, item.ReorderLevel);
    }

    [Fact]
    public void UpdateDetails_LeavesUnitOfMeasureFixed()
    {
        // User story 8: there is no path to change the unit — historical movements stay meaningful.
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);

        item.UpdateDetails("Bread flour", 1.35m, 8m);

        Assert.Equal(UnitOfMeasure.Kg, item.UnitOfMeasure);
    }

    [Fact]
    public void UpdateDetails_DoesNotDisturbCachedQuantity()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);
        item.PostMovement(MovementType.Received, 12m, DateTimeOffset.UnixEpoch);

        item.UpdateDetails("Bread flour", 1.35m, 8m);

        Assert.Equal(12m, item.QuantityOnHand);
    }

    [Fact]
    public void UpdateDetails_TrimsName()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);

        item.UpdateDetails("  Bread flour  ", 1m, 5m);

        Assert.Equal("Bread flour", item.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_RejectsBlankName(string name)
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);

        Assert.Throws<InvalidItemException>(() => item.UpdateDetails(name, 1m, 5m));
    }

    [Fact]
    public void UpdateDetails_RejectsNegativeUnitCost()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);

        Assert.Throws<InvalidItemException>(() => item.UpdateDetails("Flour", -1m, 5m));
    }

    [Fact]
    public void UpdateDetails_RejectsNegativeReorderLevel()
    {
        var item = new InventoryItem("Flour", UnitOfMeasure.Kg, 1m, 5m);

        Assert.Throws<InvalidItemException>(() => item.UpdateDetails("Flour", 1m, -1m));
    }
}
