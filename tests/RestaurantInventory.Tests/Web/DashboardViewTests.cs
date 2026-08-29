using RestaurantInventory.Core.Domain;
using RestaurantInventory.Core.Services;
using RestaurantInventory.Web.Components.Pages.Inventory;

namespace RestaurantInventory.Tests.Web;

/// <summary>
/// The dashboard's two pieces of new presentation logic (ticket 07). Both are pinned by tests
/// rather than only visible on screen:
/// <list type="bullet">
///   <item><see cref="DashboardView.TotalWasteValue"/>: the headline money figure must equal the
///   sum of the per-reason rows shown beneath it, so the total and the breakdown can never
///   disagree.</item>
///   <item><see cref="DashboardView.WindowDescription"/>: the "recent window" label must track the
///   actual window the service queried (criterion #3), not a number hard-coded in markup.</item>
/// </list>
/// </summary>
public sealed class DashboardViewTests
{
    [Fact]
    public void TotalWasteValue_SumsTheMoneyValueOfEveryReasonRow()
    {
        var rows = new[]
        {
            new WasteByReason(WasteReason.Spoiled, Quantity: 4m, MoneyValue: 10.00m),
            new WasteByReason(WasteReason.Expired, Quantity: 2m, MoneyValue: 8.00m),
            new WasteByReason(WasteReason.Other, Quantity: 1m, MoneyValue: 0.33m),
        };

        Assert.Equal(18.33m, DashboardView.TotalWasteValue(rows));
    }

    [Fact]
    public void TotalWasteValue_OfNothing_IsZero()
    {
        Assert.Equal(0m, DashboardView.TotalWasteValue(Array.Empty<WasteByReason>()));
    }

    [Fact]
    public void WindowDescription_DescribesTheWindowInWholeDays()
    {
        Assert.Equal("last 30 days", DashboardView.WindowDescription(TimeSpan.FromDays(30)));
        Assert.Equal("last 7 days", DashboardView.WindowDescription(TimeSpan.FromDays(7)));
    }

    [Fact]
    public void WindowDescription_MatchesTheServiceDefaultWindow()
    {
        // The label the page shows and the window the service actually queries must not drift apart.
        Assert.Equal("last 30 days", DashboardView.WindowDescription(InventoryService.DefaultWasteWindow));
    }
}
