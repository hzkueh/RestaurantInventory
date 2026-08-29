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
            "write a concise briefing of the current state as a short list of bullet points. Cover " +
            "what is in shortage and needs reordering, notable recent stock movements, and what waste " +
            "has cost. Rules: output only the bullet lines, with no heading, intro, or closing line. " +
            "Start each bullet on its own line with '- '. Keep each bullet to one short sentence that " +
            "states the result directly and names specific items and figures. Aim for at most six " +
            "bullets. Do not invent data beyond what is given, and use no other Markdown formatting.");
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

    private static string Money(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Unit(UnitOfMeasure unit) => unit.Abbreviate();
}
