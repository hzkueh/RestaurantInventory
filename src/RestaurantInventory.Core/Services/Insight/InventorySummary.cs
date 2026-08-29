namespace RestaurantInventory.Core.Services.Insight;

/// <summary>
/// The result of an AI briefing request: either an available <see cref="Briefing"/>, or an
/// "unavailable" state carrying a Manager-facing <see cref="UnavailableReason"/>. Modelling the
/// two outcomes as one closed result — rather than a nullable value or an exception — keeps the
/// graceful-degradation path (user story 29) explicit at every call site.
/// </summary>
public sealed record InventorySummary
{
    private InventorySummary(bool isAvailable, InventoryBriefing? briefing, string? unavailableReason)
    {
        IsAvailable = isAvailable;
        Briefing = briefing;
        UnavailableReason = unavailableReason;
    }

    /// <summary>True when a <see cref="Briefing"/> was generated; false when the feature is unavailable.</summary>
    public bool IsAvailable { get; }

    /// <summary>The structured briefing when <see cref="IsAvailable"/>; otherwise <c>null</c>.</summary>
    public InventoryBriefing? Briefing { get; }

    /// <summary>Why no briefing is available (e.g. no key configured), for display; <c>null</c> when available.</summary>
    public string? UnavailableReason { get; }

    /// <summary>A successful briefing carrying the generated content.</summary>
    public static InventorySummary Available(InventoryBriefing briefing) => new(true, briefing, null);

    /// <summary>No briefing — the AI feature is unavailable for the given, Manager-facing reason.</summary>
    public static InventorySummary Unavailable(string reason) => new(false, null, reason);
}
