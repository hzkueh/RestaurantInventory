using RestaurantInventory.Web.Components.Pages.Inventory;

namespace RestaurantInventory.Tests.Web;

/// <summary>
/// The AI summary page renders the model's narrative as bullet points, so the parsing of that
/// loosely-shaped text (<see cref="SummaryView.Bullets"/>) is pinned here: markers are stripped,
/// blank lines dropped, and an empty narrative yields nothing so the page can fall back.
/// </summary>
public sealed class SummaryViewTests
{
    [Fact]
    public void Bullets_SplitsLinesAndStripsDashMarkers()
    {
        var bullets = SummaryView.Bullets("- Milk is short at 25 L.\n- Oreos are out of stock.");

        Assert.Equal(
            new[] { "Milk is short at 25 L.", "Oreos are out of stock." },
            bullets.ToArray());
    }

    [Fact]
    public void Bullets_StripsAssortedMarkersAndNumbering()
    {
        var bullets = SummaryView.Bullets("• Spoiled waste cost $6.30.\n2. Received 20 kg rice.\n* Adjusted flour down 1 kg.");

        Assert.Equal(
            new[] { "Spoiled waste cost $6.30.", "Received 20 kg rice.", "Adjusted flour down 1 kg." },
            bullets.ToArray());
    }

    [Fact]
    public void Bullets_StripsWrappingBoldEmphasis()
    {
        var bullets = SummaryView.Bullets("- **Reorder milk now.**");

        Assert.Equal(new[] { "Reorder milk now." }, bullets.ToArray());
    }

    [Fact]
    public void Bullets_DropsBlankLines()
    {
        var bullets = SummaryView.Bullets("- One.\n\n   \n- Two.\n");

        Assert.Equal(new[] { "One.", "Two." }, bullets.ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  ")]
    public void Bullets_OfNothing_IsEmpty(string? narrative)
    {
        Assert.Empty(SummaryView.Bullets(narrative));
    }
}
