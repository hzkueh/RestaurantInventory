using RestaurantInventory.Core.Domain;
using RestaurantInventory.Web.Components.Pages.Inventory;

namespace RestaurantInventory.Tests.Web;

/// <summary>
/// The item-detail audit formatting (review finding #1): the signed ledger change, the reason
/// text, and the newest-first ordering. This is the point of user story 14 — "audit exactly how
/// the current quantity was reached" — so a regression that flips a sign or a sort direction must
/// fail a test, not just look wrong to whoever happens to open the page.
/// </summary>
public sealed class MovementDisplayTests
{
    private static readonly DateTimeOffset T = new(2026, 8, 26, 9, 0, 0, TimeSpan.Zero);

    private static StockMovement Movement(
        MovementType type,
        decimal quantity,
        DateTimeOffset? timestamp = null,
        string? reason = null,
        WasteReason? wasteReason = null,
        string? note = null)
        => new(inventoryItemId: 1, type, quantity, timestamp ?? T, reason, wasteReason, note);

    [Fact]
    public void SignedDelta_MatchesLedgerEffect_PerType()
    {
        Assert.Equal(10m, MovementDisplay.SignedDelta(Movement(MovementType.Received, 10m)));
        Assert.Equal(-3m, MovementDisplay.SignedDelta(Movement(MovementType.Wasted, 3m, wasteReason: WasteReason.Spoiled)));
        Assert.Equal(4m, MovementDisplay.SignedDelta(Movement(MovementType.Adjusted, 4m, reason: "recount")));
        Assert.Equal(-4m, MovementDisplay.SignedDelta(Movement(MovementType.Adjusted, -4m, reason: "recount")));
    }

    [Theory]
    [InlineData(MovementType.Received, 25, "+25 kg")]
    [InlineData(MovementType.Wasted, 2, "-2 kg")]
    public void ChangeText_SignsAndFormatsWithUnit(MovementType type, double quantity, string expected)
    {
        var wasteReason = type == MovementType.Wasted ? (WasteReason?)WasteReason.Spoiled : null;
        var text = MovementDisplay.ChangeText(Movement(type, (decimal)quantity, wasteReason: wasteReason), UnitOfMeasure.Kg);
        Assert.Equal(expected, text);
    }

    [Fact]
    public void ChangeText_Adjusted_ShowsSignBothWays()
    {
        Assert.Equal("+3 L", MovementDisplay.ChangeText(Movement(MovementType.Adjusted, 3m, reason: "recount"), UnitOfMeasure.L));
        Assert.Equal("-5 L", MovementDisplay.ChangeText(Movement(MovementType.Adjusted, -5m, reason: "recount"), UnitOfMeasure.L));
    }

    [Fact]
    public void ReasonText_Waste_CombinesReasonAndNote()
    {
        var m = Movement(MovementType.Wasted, 2m, wasteReason: WasteReason.Spoiled, note: "weevils");
        Assert.Equal("Spoiled — weevils", MovementDisplay.ReasonText(m));
    }

    [Fact]
    public void ReasonText_Waste_WithoutNote_ShowsReasonOnly()
    {
        var m = Movement(MovementType.Wasted, 2m, wasteReason: WasteReason.Spilled);
        Assert.Equal("Spilled", MovementDisplay.ReasonText(m));
    }

    [Fact]
    public void ReasonText_NonWaste_ShowsFreeTextReason()
    {
        Assert.Equal("physical count", MovementDisplay.ReasonText(Movement(MovementType.Adjusted, -1m, reason: "physical count")));
        Assert.Equal("", MovementDisplay.ReasonText(Movement(MovementType.Received, 5m)));
    }

    [Fact]
    public void NewestFirst_OrdersByTimestampDescending()
    {
        var oldest = Movement(MovementType.Received, 1m, timestamp: T);
        var middle = Movement(MovementType.Received, 1m, timestamp: T.AddHours(1));
        var newest = Movement(MovementType.Received, 1m, timestamp: T.AddHours(2));

        var ordered = MovementDisplay.NewestFirst(new[] { oldest, newest, middle });

        Assert.Equal(new[] { newest, middle, oldest }, ordered);
    }
}
