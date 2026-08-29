using System.Globalization;
using System.Text;
using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Core.Services.Insight;

/// <summary>
/// Builds the single LLM prompt from an <see cref="InventoryStateReport"/>. Kept pure and
/// side-effect-free (like <c>DashboardView</c> and <c>MovementForm</c>) so what the AI is asked —
/// and that it reflects real inventory state — is unit-testable without a network (ADR-0002).
/// </summary>
public static class InsightPrompt
{
    /// <summary>
    /// Assembles the instruction and the current-state data into one prompt. Empty sections are
    /// spelled out as "None" rather than left blank, so the model is never handed a dangling header
    /// to invent content under.
    /// </summary>
    public static string Build(InventoryStateReport report)
    {
        var windowDays = report.WasteWindow.TotalDays.ToString("0.#", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.AppendLine(
            "You are an assistant to a restaurant Manager. Using only the inventory data below, " +
            "produce a concise briefing of the current state as a single JSON object with exactly " +
            "this shape:");
        sb.AppendLine("""
            {
              "headline": "one short sentence summarising the overall state",
              "shortages": ["one short bullet per item that needs reordering"],
              "recentMovements": ["one short bullet per notable recent movement"],
              "waste": ["one short bullet per waste reason, stating what it cost"]
            }
            """);
        sb.AppendLine(
            "Rules: each bullet is one short sentence that states the result directly and names " +
            "specific items and figures. Reproduce every money figure exactly as it appears below — " +
            "it is already currency-formatted; do not add quotation marks or any other characters " +
            "around it. If a section has nothing to report, use an empty array. Aim for at most six " +
            "bullets per section. Do not invent data beyond what is given. Output only the JSON " +
            "object, with no surrounding text or Markdown.");
        sb.AppendLine();

        sb.AppendLine("## Items in shortage (quantity on hand at or below reorder level)");
        if (report.Shortages.Count == 0)
        {
            sb.AppendLine("None — every item is above its reorder level.");
        }
        else
        {
            foreach (var s in report.Shortages)
            {
                sb.AppendLine(
                    $"- {s.Name}: {Number(s.QuantityOnHand)} {Unit(s.Unit)} on hand " +
                    $"(reorder level {Number(s.ReorderLevel)} {Unit(s.Unit)})");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Recent stock movements (newest first)");
        if (report.RecentMovements.Count == 0)
        {
            sb.AppendLine("None recorded.");
        }
        else
        {
            foreach (var m in report.RecentMovements)
            {
                var when = m.Timestamp.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var detail = string.IsNullOrWhiteSpace(m.Detail) ? "" : $" — {m.Detail}";
                sb.AppendLine(
                    $"- {when}: {m.Type} {Number(m.Quantity)} {Unit(m.Unit)} of {m.ItemName}{detail}");
            }
        }
        sb.AppendLine();

        sb.AppendLine($"## Waste by reason (last {windowDays} days, valued in money)");
        if (report.WasteByReason.Count == 0)
        {
            sb.AppendLine("None recorded in the window.");
        }
        else
        {
            foreach (var w in report.WasteByReason)
            {
                sb.AppendLine($"- {w.Reason}: {Money(w.MoneyValue)} ({Number(w.Quantity)} total)");
            }
        }

        return sb.ToString();
    }

    private static string Number(decimal value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    // Money is written currency-formatted so the model echoes it back already formatted (e.g. $150.00);
    // uses the same current-culture "C" format as the dashboard so the two screens agree.
    private static string Money(decimal value)
        => value.ToString("C", CultureInfo.CurrentCulture);

    private static string Unit(UnitOfMeasure unit) => unit.Abbreviate();
}
