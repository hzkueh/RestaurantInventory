namespace RestaurantInventory.Core.Services.Insight;

/// <summary>
/// The application-owned seam for the on-demand AI briefing (per ADR-0002). The summary page
/// depends only on this interface, never on a concrete provider, so swapping Gemini for another
/// LLM is a single new implementation. Implementations gather current inventory state and turn it
/// into a plain-language narrative for the Manager.
/// </summary>
/// <remarks>
/// The feature is deliberately optional and off the critical path: when no provider is configured
/// (or a call fails) an implementation returns an <see cref="InventorySummary.Unavailable"/> result
/// rather than throwing, so a missing key never breaks the rest of the app (user story 29).
/// </remarks>
public interface IInventoryInsightService
{
    /// <summary>
    /// Produces a narrative briefing of current inventory state — Shortages, recent movements, and
    /// Waste totals — from a single LLM call. Called only when the Manager asks (user story 28), so
    /// no call is made on page load. Never throws for an expected "no provider / call failed" case;
    /// those surface as an <see cref="InventorySummary.Unavailable"/> result.
    /// </summary>
    Task<InventorySummary> SummariseAsync(CancellationToken cancellationToken = default);
}
