using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;
using RestaurantInventory.Core.Services.Insight;

namespace RestaurantInventory.Web.Infrastructure;

/// <summary>
/// The Gemini implementation of the AI-briefing seam (ADR-0002): gathers current inventory state
/// through the <see cref="InventoryService"/>, turns it into a single prompt, and calls the Gemini
/// REST endpoint with a typed <see cref="HttpClient"/> (via <c>IHttpClientFactory</c>) — no
/// third-party AI SDK. Kept deliberately off the critical path: with no key configured, or on any
/// call failure, it returns an <see cref="InventorySummary.Unavailable"/> result rather than
/// throwing, so the rest of the app is unaffected (user story 29).
/// </summary>
public sealed class GeminiInsightService : IInventoryInsightService
{
    /// <summary>How many recent movements to feed the briefing — bounded so the prompt stays small.</summary>
    private const int RecentMovementLimit = 20;

    private const string NoKeyMessage =
        "The AI summary is unavailable because no AI provider key is configured. " +
        "Everything else in the app works as normal; add a key to enable it (see .env.example).";

    private const string CallFailedMessage =
        "The AI summary could not be generated right now. Please try again in a moment — " +
        "the rest of the app is unaffected.";

    private readonly HttpClient _http;
    private readonly InventoryService _inventory;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiInsightService> _logger;

    public GeminiInsightService(
        HttpClient http,
        InventoryService inventory,
        IOptions<GeminiOptions> options,
        ILogger<GeminiInsightService> logger)
    {
        _http = http;
        _inventory = inventory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<InventorySummary> SummariseAsync(CancellationToken cancellationToken = default)
    {
        // Short-circuit before touching the database or the network: no key means the feature is off.
        if (!_options.IsConfigured)
            return InventorySummary.Unavailable(NoKeyMessage);

        try
        {
            var report = await GatherStateAsync(cancellationToken);
            var prompt = InsightPrompt.Build(report);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"v1beta/models/{_options.ResolvedModelId}:generateContent");
            // Key travels in a header, never the URL/query string.
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = JsonContent.Create(GeminiRequest.ForPrompt(prompt));

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini insight call failed with status {StatusCode}.", (int)response.StatusCode);
                return InventorySummary.Unavailable(CallFailedMessage);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var briefingJson = GeminiResponseReader.ReadNarrative(json);
            var briefing = briefingJson is null ? null : InventoryBriefingReader.Read(briefingJson);
            if (briefing is null)
            {
                _logger.LogWarning("Gemini insight response carried no usable briefing.");
                return InventorySummary.Unavailable(CallFailedMessage);
            }

            return InventorySummary.Available(briefing);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A genuine caller cancellation (e.g. the Manager navigated away) is not a feature failure.
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            // Only the expected transport failures degrade to "unavailable" (a network error, a
            // timeout — surfacing here as a non-caller OperationCanceledException — or a malformed
            // body). Anything else is a real bug and is left to propagate rather than be hidden.
            _logger.LogWarning(ex, "Gemini insight call failed; degrading to an unavailable summary.");
            return InventorySummary.Unavailable(CallFailedMessage);
        }
    }

    private async Task<InventoryStateReport> GatherStateAsync(CancellationToken cancellationToken)
    {
        var shortages = await _inventory.GetShortagesAsync(cancellationToken);
        var movements = await _inventory.GetRecentMovementsAsync(RecentMovementLimit, cancellationToken);
        var waste = await _inventory.GetWasteByReasonAsync(cancellationToken: cancellationToken);

        var shortageLines = shortages
            .Select(i => new ShortageLine(i.Name, i.QuantityOnHand, i.ReorderLevel, i.UnitOfMeasure))
            .ToList();

        var movementLines = movements
            .Select(m => new MovementLine(
                m.Timestamp,
                m.InventoryItem?.Name ?? "(unknown item)",
                m.Type,
                m.Quantity,
                m.InventoryItem?.UnitOfMeasure ?? UnitOfMeasure.Each,
                MovementDetail(m)))
            .ToList();

        return new InventoryStateReport(shortageLines, movementLines, waste, InventoryService.DefaultWasteWindow);
    }

    /// <summary>The human-readable extra a movement carries: waste reason (+ note), or an adjustment reason.</summary>
    private static string? MovementDetail(StockMovement movement) => movement.Type switch
    {
        MovementType.Wasted => string.IsNullOrWhiteSpace(movement.Note)
            ? movement.WasteReason?.ToString()
            : $"{movement.WasteReason} ({movement.Note})",
        MovementType.Adjusted => movement.Reason,
        _ => null,
    };
}
