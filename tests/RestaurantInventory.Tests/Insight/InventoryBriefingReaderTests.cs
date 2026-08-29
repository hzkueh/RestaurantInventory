using RestaurantInventory.Web.Infrastructure;

namespace RestaurantInventory.Tests.Insight;

/// <summary>
/// Parsing the model's JSON into a structured <see cref="RestaurantInventory.Core.Services.Insight.InventoryBriefing"/>
/// is the piece of the response path with real logic, pinned here as a pure function of the raw
/// JSON (no network, per ADR-0002). Malformed or empty output must yield <c>null</c> so the caller
/// degrades to "unavailable" rather than showing an empty card.
/// </summary>
public sealed class InventoryBriefingReaderTests
{
    [Fact]
    public void Read_MapsHeadlineAndAllThreeGroups()
    {
        const string json = """
            {
              "headline": "Two items need reordering.",
              "shortages": ["Milk is down to 25 L."],
              "recentMovements": ["Received 20 kg rice."],
              "waste": ["Expired waste cost $150.00."]
            }
            """;

        var briefing = InventoryBriefingReader.Read(json);

        Assert.NotNull(briefing);
        Assert.Equal("Two items need reordering.", briefing!.Headline);
        Assert.Equal(new[] { "Milk is down to 25 L." }, briefing.Shortages.ToArray());
        Assert.Equal(new[] { "Received 20 kg rice." }, briefing.RecentMovements.ToArray());
        Assert.Equal(new[] { "Expired waste cost $150.00." }, briefing.Waste.ToArray());
    }

    [Fact]
    public void Read_TreatsMissingGroupsAsEmpty()
    {
        var briefing = InventoryBriefingReader.Read("""{ "headline": "All stocked." }""");

        Assert.NotNull(briefing);
        Assert.Equal("All stocked.", briefing!.Headline);
        Assert.Empty(briefing.Shortages);
        Assert.Empty(briefing.RecentMovements);
        Assert.Empty(briefing.Waste);
    }

    [Fact]
    public void Read_DropsBlankBulletsAndTrims()
    {
        const string json = """
            { "headline": " Heads up. ", "shortages": ["  Milk short.  ", "", "   "] }
            """;

        var briefing = InventoryBriefingReader.Read(json);

        Assert.Equal("Heads up.", briefing!.Headline);
        Assert.Equal(new[] { "Milk short." }, briefing.Shortages.ToArray());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{ }")]
    [InlineData("""{ "headline": "  ", "shortages": [], "recentMovements": [], "waste": [] }""")]
    public void Read_ReturnsNullForUnusableOutput(string json)
    {
        Assert.Null(InventoryBriefingReader.Read(json));
    }
}
