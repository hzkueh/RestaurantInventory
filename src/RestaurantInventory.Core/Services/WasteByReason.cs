using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Core.Services;

/// <summary>
/// Total Waste for a single <see cref="WasteReason"/> over a window: how much stock was
/// wasted and what it cost, valued at each item's <see cref="InventoryItem.UnitCost"/>.
/// </summary>
/// <param name="Reason">The reason the waste was booked under.</param>
/// <param name="Quantity">Sum of wasted quantities (mixed units — informational only).</param>
/// <param name="MoneyValue">Money lost, rounded to two decimal places.</param>
public sealed record WasteByReason(WasteReason Reason, decimal Quantity, decimal MoneyValue);
