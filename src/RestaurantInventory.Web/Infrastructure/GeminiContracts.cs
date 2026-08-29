using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestaurantInventory.Web.Infrastructure;

// Minimal request/response shapes for the Gemini generateContent REST endpoint. Hand-rolled DTOs
// (no third-party AI SDK, per ADR-0002); only the handful of fields this feature uses are modelled.

/// <summary>Request body for <c>:generateContent</c>: one user turn plus a JSON-output directive.</summary>
public sealed record GeminiRequest(
    [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig)
{
    public static GeminiRequest ForPrompt(string prompt)
        => new(
            new[] { new GeminiContent(new[] { new GeminiPart(prompt) }) },
            new GeminiGenerationConfig("application/json"));
}

/// <summary>Generation options — here, asking the model to return a JSON body so it parses reliably.</summary>
public sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("responseMimeType")] string ResponseMimeType);

public sealed record GeminiContent(
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

public sealed record GeminiPart(
    [property: JsonPropertyName("text")] string Text);

public sealed record GeminiResponse(
    [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates);

public sealed record GeminiCandidate(
    [property: JsonPropertyName("content")] GeminiContent? Content);

/// <summary>
/// Pulls the narrative text out of a Gemini response. Kept a pure function of the raw JSON so the
/// parsing — the one piece of the HTTP path with real logic — is unit-testable without a network
/// (ADR-0002: the live endpoint is never exercised in tests).
/// </summary>
public static class GeminiResponseReader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The concatenated text of the first candidate's parts, trimmed; <c>null</c> when the response
    /// carries no usable text (empty candidates, no parts, or blank text) or is not valid JSON.
    /// Returning null rather than throwing lets the caller fall back to an "unavailable" result.
    /// </summary>
    public static string? ReadNarrative(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        GeminiResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<GeminiResponse>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }

        var parts = response?.Candidates is { Count: > 0 } candidates
            ? candidates[0].Content?.Parts
            : null;
        if (parts is null || parts.Count == 0)
            return null;

        var text = string.Concat(parts.Select(p => p.Text)).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
