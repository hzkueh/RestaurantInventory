using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>
/// How a <see cref="UnitOfMeasure"/> is written for the Manager. Kept in one place so the
/// list, detail, and any later movement screens all abbreviate units the same way.
/// </summary>
public static class UnitDisplay
{
    public static string Abbreviation(UnitOfMeasure unit) => unit.Abbreviate();

    /// <summary>A quantity with its unit, e.g. <c>12.5 kg</c>. Trailing zeros are trimmed.</summary>
    public static string Quantity(decimal quantity, UnitOfMeasure unit)
        => $"{quantity.ToString("0.###")} {Abbreviation(unit)}";
}
