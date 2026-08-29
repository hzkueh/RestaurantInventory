using RestaurantInventory.Core.Services.Insight;
using RestaurantInventory.Web.Components.Pages.Inventory;

namespace RestaurantInventory.Tests.Web;

/// <summary>
/// The AI summary page's behaviour (ticket 08) tested against a fake <see cref="IInventoryInsightService"/>,
/// with no network involved (ADR-0002). The page is a thin caller of <see cref="SummaryPresenter"/>,
/// which owns the on-demand call and the three display states — so both headline behaviours are pinned:
/// the briefing is generated only when asked (user story 28), and a missing key degrades gracefully to
/// an "unavailable" message rather than an error (user story 29).
/// </summary>
public sealed class SummaryPresenterTests
{
    /// <summary>A hand-rolled stand-in for the seam (the repo uses fakes, not a mocking library).</summary>
    private sealed class FakeInsightService : IInventoryInsightService
    {
        private readonly InventorySummary _result;
        public FakeInsightService(InventorySummary result) => _result = result;
        public int Calls { get; private set; }

        public Task<InventorySummary> SummariseAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_result);
        }
    }

    private static InventoryBriefing Briefing(string headline = "All good.")
        => new(headline, new[] { "Milk is short." }, Array.Empty<string>(), new[] { "Spoiled waste cost $6.30." });

    [Fact]
    public void BeforeGenerating_IsIdleAndHasNotCalledTheService()
    {
        var fake = new FakeInsightService(InventorySummary.Available(Briefing()));
        var presenter = new SummaryPresenter(fake);

        Assert.Equal(SummaryState.Idle, presenter.State);
        Assert.Equal(0, fake.Calls); // user story 28: no call on load, only on demand.
    }

    [Fact]
    public async Task Generate_WithAnAvailableSummary_ExposesTheBriefing()
    {
        var fake = new FakeInsightService(InventorySummary.Available(Briefing("Tomatoes are low.")));
        var presenter = new SummaryPresenter(fake);

        await presenter.GenerateAsync();

        Assert.Equal(1, fake.Calls);
        Assert.Equal(SummaryState.Available, presenter.State);
        Assert.Equal("Tomatoes are low.", presenter.Briefing!.Headline);
        Assert.Null(presenter.UnavailableReason);
    }

    [Fact]
    public async Task Generate_WhenUnavailable_ExposesTheReasonAndNoBriefing()
    {
        var fake = new FakeInsightService(InventorySummary.Unavailable("No AI key configured."));
        var presenter = new SummaryPresenter(fake);

        await presenter.GenerateAsync();

        Assert.Equal(SummaryState.Unavailable, presenter.State);
        Assert.Equal("No AI key configured.", presenter.UnavailableReason);
        Assert.Null(presenter.Briefing);
    }

    [Fact]
    public async Task Regenerating_AfterUnavailable_ClearsTheStaleReason()
    {
        // A page kept open across a config change (key added) must not show both a briefing and a
        // leftover "unavailable" reason: each generate fully replaces the previous outcome.
        var unavailable = new FakeInsightService(InventorySummary.Unavailable("No AI key configured."));
        var presenter = new SummaryPresenter(unavailable);
        await presenter.GenerateAsync();

        var available = new FakeInsightService(InventorySummary.Available(Briefing()));
        presenter = new SummaryPresenter(available);
        await presenter.GenerateAsync();

        Assert.Equal(SummaryState.Available, presenter.State);
        Assert.NotNull(presenter.Briefing);
        Assert.Null(presenter.UnavailableReason);
    }
}
