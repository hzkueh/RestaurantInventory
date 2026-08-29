namespace RestaurantInventory.Web.Infrastructure;

/// <summary>
/// Configuration for the Gemini insight provider (ADR-0002), bound from the <c>Gemini</c> section.
/// The values come from config — in practice a git-ignored <c>.env</c> (see <c>.env.example</c>) —
/// so no key is ever committed.
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Default model when config supplies none — the free-tier model verified in ADR-0002.</summary>
    public const string DefaultModelId = "gemini-3.1-flash-lite";

    /// <summary>The Google Gemini API key. Absent in a default clone; the feature degrades gracefully.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The model to call; falls back to <see cref="DefaultModelId"/> when unset.</summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Whether the provider can make a call. Keyed on the API key alone: with no key the AI page
    /// shows an "unavailable" state and the rest of the app is unaffected (user story 29).
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>The configured model, or <see cref="DefaultModelId"/> if none was supplied.</summary>
    public string ResolvedModelId => string.IsNullOrWhiteSpace(ModelId) ? DefaultModelId : ModelId;
}
