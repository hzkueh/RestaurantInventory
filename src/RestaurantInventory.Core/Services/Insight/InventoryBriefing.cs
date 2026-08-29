namespace RestaurantInventory.Core.Services.Insight;

/// <summary>
/// A structured AI briefing: a one-line <see cref="Headline"/> verdict followed by three labelled
/// groups of one-line bullets — Shortages first, then recent movements, then waste. Structuring the
/// result (rather than returning one free-text blob) lets the page own the headline styling, the
/// group labels, and their order, and keeps money figures formatted as they were fed to the model.
/// </summary>
public sealed record InventoryBriefing(
    string Headline,
    IReadOnlyList<string> Shortages,
    IReadOnlyList<string> RecentMovements,
    IReadOnlyList<string> Waste);
