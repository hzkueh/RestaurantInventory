using System.Text.Json;
using System.Text.Json.Serialization;
using RestaurantInventory.Core.Services.Insight;

namespace RestaurantInventory.Web.Infrastructure;

/// <summary>
/// Turns the JSON text the model returns into a structured <see cref="InventoryBriefing"/>. Kept a
/// pure function of the raw JSON so the parsing is unit-testable without a network (ADR-0002).
/// Returns <c>null</c> for unusable output (not JSON, or no headline and no bullets at all) so the
/// caller can fall back to an "unavailable" result rather than showing an empty card.
/// </summary>
public static class InventoryBriefingReader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static InventoryBriefing? Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto is null)
            return null;

        var headline = dto.Headline?.Trim() ?? string.Empty;
        var shortages = Clean(dto.Shortages);
        var movements = Clean(dto.RecentMovements);
        var waste = Clean(dto.Waste);

        // Nothing usable came back — treat as a failed generation, not an empty briefing.
        if (headline.Length == 0 && shortages.Count == 0 && movements.Count == 0 && waste.Count == 0)
            return null;

        return new InventoryBriefing(headline, shortages, movements, waste);
    }

    /// <summary>Trims each bullet and drops blanks, tolerating a null array or null entries.</summary>
    private static IReadOnlyList<string> Clean(IReadOnlyList<string?>? items)
    {
        if (items is null)
            return Array.Empty<string>();

        return items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }

    private sealed record Dto(
        [property: JsonPropertyName("headline")] string? Headline,
        [property: JsonPropertyName("shortages")] IReadOnlyList<string?>? Shortages,
        [property: JsonPropertyName("recentMovements")] IReadOnlyList<string?>? RecentMovements,
        [property: JsonPropertyName("waste")] IReadOnlyList<string?>? Waste);
}
