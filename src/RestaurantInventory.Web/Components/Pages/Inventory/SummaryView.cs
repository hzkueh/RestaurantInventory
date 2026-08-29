namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>
/// Pure presentation helper for the AI summary page (ticket 08): turns the model's narrative — one
/// concise result per line — into the bullet items the page renders. Kept side-effect-free and
/// unit-tested (like <c>DashboardView</c>) so the parsing of the LLM's loosely-shaped text is
/// pinned, not just visible on screen.
/// </summary>
public static class SummaryView
{
    private static readonly char[] BulletMarkers = { '-', '*', '•', '·', '–', '—' };

    /// <summary>
    /// Splits the narrative into trimmed bullet lines, stripping any leading list marker the model
    /// added ('-', '*', '•', a numbered "1.") and surrounding <c>**</c> emphasis. Blank lines are
    /// dropped. An empty or whitespace narrative yields no bullets, so the caller can fall back.
    /// </summary>
    public static IReadOnlyList<string> Bullets(string? narrative)
    {
        if (string.IsNullOrWhiteSpace(narrative))
            return Array.Empty<string>();

        var bullets = new List<string>();
        foreach (var rawLine in narrative.Split('\n'))
        {
            var line = StripMarker(rawLine.Trim());
            if (line.Length > 0)
                bullets.Add(line);
        }

        return bullets;
    }

    private static string StripMarker(string line)
    {
        // Drop a leading "1." / "2)" style numbered marker, if any.
        var i = 0;
        while (i < line.Length && char.IsDigit(line[i]))
            i++;
        if (i > 0 && i < line.Length && (line[i] == '.' || line[i] == ')'))
            line = line[(i + 1)..].TrimStart();

        // Drop a leading bullet glyph.
        line = line.TrimStart(BulletMarkers).TrimStart();

        // Drop wrapping bold emphasis the model may have added despite the instruction.
        if (line.Length >= 4 && line.StartsWith("**", StringComparison.Ordinal) &&
            line.EndsWith("**", StringComparison.Ordinal))
        {
            line = line[2..^2].Trim();
        }

        return line;
    }
}
