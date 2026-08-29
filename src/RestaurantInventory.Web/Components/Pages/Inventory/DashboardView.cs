using RestaurantInventory.Core.Services;

namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>
/// Pure presentation helpers for the Manager dashboard (ticket 07). The dashboard reads entirely
/// from the inventory service seam; the only new logic is derived from those results — the grand
/// total across the Waste-by-reason rows and the label for the recent window — so it lives here,
/// side-effect-free and unit-tested, rather than inline in the page markup.
/// </summary>
public static class DashboardView
{
    /// <summary>
    /// The headline Waste figure: the sum of the money value of every <see cref="WasteByReason"/>
    /// row. Each row's value is already rounded to two places by
    /// <see cref="InventoryService.SummariseWaste"/>, so summing them keeps the total exactly equal
    /// to the breakdown shown beneath it — the total can never disagree with its own rows.
    /// </summary>
    public static decimal TotalWasteValue(IEnumerable<WasteByReason> wasteByReason)
        => wasteByReason.Sum(w => w.MoneyValue);

    /// <summary>
    /// The recent window written for the Manager, e.g. <c>last 30 days</c>. Derived from the same
    /// <see cref="TimeSpan"/> the service queried (criterion #3) so the label and the figures never
    /// drift apart.
    /// </summary>
    public static string WindowDescription(TimeSpan window)
        => $"last {window.TotalDays:0.#} days";
}
