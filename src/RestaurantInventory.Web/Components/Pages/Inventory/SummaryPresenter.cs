using RestaurantInventory.Core.Services.Insight;

namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>Where the AI summary page is in its on-demand lifecycle.</summary>
public enum SummaryState
{
    /// <summary>Nothing requested yet — the page shows the "Generate" prompt and has made no call.</summary>
    Idle,

    /// <summary>A briefing has been requested and the call is in flight.</summary>
    Generating,

    /// <summary>A briefing was generated and is being shown.</summary>
    Available,

    /// <summary>The feature reported itself unavailable (e.g. no key, or a failed call).</summary>
    Unavailable,
}

/// <summary>
/// The on-demand logic behind the AI summary page (ticket 08), extracted from the <c>.razor</c> so
/// it is unit-testable against a mocked <see cref="IInventoryInsightService"/> without a network
/// (ADR-0002) — mirroring the <c>DashboardView</c>/<c>MovementForm</c> pattern of keeping the page
/// a thin caller. Owns the call and maps its two outcomes onto the display <see cref="State"/>.
/// </summary>
public sealed class SummaryPresenter
{
    private readonly IInventoryInsightService _insight;

    public SummaryPresenter(IInventoryInsightService insight) => _insight = insight;

    public SummaryState State { get; private set; } = SummaryState.Idle;

    /// <summary>The generated briefing when <see cref="State"/> is <see cref="SummaryState.Available"/>.</summary>
    public InventoryBriefing? Briefing { get; private set; }

    /// <summary>Why no briefing is shown when <see cref="State"/> is <see cref="SummaryState.Unavailable"/>.</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>
    /// Requests a briefing — the only thing that calls the service, so no call happens until the
    /// Manager asks (user story 28). Each call fully replaces the previous outcome, so a stale
    /// briefing or "unavailable" reason can never linger alongside a new result.
    /// </summary>
    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        State = SummaryState.Generating;
        var summary = await _insight.SummariseAsync(cancellationToken);
        if (summary.IsAvailable)
        {
            Briefing = summary.Briefing;
            UnavailableReason = null;
            State = SummaryState.Available;
        }
        else
        {
            UnavailableReason = summary.UnavailableReason;
            Briefing = null;
            State = SummaryState.Unavailable;
        }
    }
}
