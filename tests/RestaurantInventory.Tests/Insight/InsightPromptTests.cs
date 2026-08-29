using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;
using RestaurantInventory.Core.Services.Insight;

namespace RestaurantInventory.Tests.Insight;

/// <summary>
/// The prompt sent to the LLM is built by a pure function of the inventory snapshot
/// (<see cref="InsightPrompt.Build"/>), so what the AI is asked — and that it reflects real state —
/// is pinned by tests without touching the network (ADR-0002: testable without a provider).
/// </summary>
public sealed class InsightPromptTests
{
    private static InventoryStateReport Report(
        IReadOnlyList<ShortageLine>? shortages = null,
        IReadOnlyList<MovementLine>? movements = null,
        IReadOnlyList<WasteByReason>? waste = null,
        TimeSpan? window = null)
        => new(
            shortages ?? Array.Empty<ShortageLine>(),
            movements ?? Array.Empty<MovementLine>(),
            waste ?? Array.Empty<WasteByReason>(),
            window ?? TimeSpan.FromDays(30));

    [Fact]
    public void Build_IncludesEachShortageWithItsQuantityAndLevel()
    {
        var prompt = InsightPrompt.Build(Report(
            shortages: new[] { new ShortageLine("Tomatoes", QuantityOnHand: 2m, ReorderLevel: 5m, UnitOfMeasure.Kg) }));

        Assert.Contains("Tomatoes", prompt);
        Assert.Contains("2", prompt);
        Assert.Contains("5", prompt);
    }

    [Fact]
    public void Build_IncludesEachWasteReasonWithItsMoneyValue_CurrencyFormatted()
    {
        var prompt = InsightPrompt.Build(Report(
            waste: new[] { new WasteByReason(WasteReason.Spoiled, Quantity: 3m, MoneyValue: 12.50m) }));

        Assert.Contains("Spoiled", prompt);
        // Money is fed to the model already currency-formatted (same "C" format as the dashboard),
        // so it echoes it back formatted rather than as a bare number.
        Assert.Contains(12.50m.ToString("C"), prompt);
    }

    [Fact]
    public void Build_IncludesRecentMovementsByItemName()
    {
        var prompt = InsightPrompt.Build(Report(
            movements: new[]
            {
                new MovementLine(
                    DateTimeOffset.UnixEpoch, "Olive Oil", MovementType.Received, 10m, UnitOfMeasure.L, Detail: null),
            }));

        Assert.Contains("Olive Oil", prompt);
        Assert.Contains("Received", prompt);
    }

    [Fact]
    public void Build_StatesTheWasteWindowInDays()
    {
        var prompt = InsightPrompt.Build(Report(window: TimeSpan.FromDays(30)));

        Assert.Contains("30", prompt);
    }

    [Fact]
    public void Build_WithNothingToReport_SaysSoRatherThanLeavingSectionsBlank()
    {
        // An all-empty snapshot must still yield a usable prompt: the LLM should be told each section
        // is empty, not handed dangling headers it might hallucinate content under.
        var prompt = InsightPrompt.Build(Report());

        Assert.False(string.IsNullOrWhiteSpace(prompt));
        Assert.Contains("None", prompt);
    }
}
