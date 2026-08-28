using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>
/// The type-aware rules that shape the record-movement form (ticket 06): which optional fields
/// apply to which <see cref="MovementType"/>. Kept as pure predicates in one place so the form's
/// headline behaviour is unit-tested rather than only asserted by eye, and so the page markup
/// reads as thin conditionals over a named rule instead of scattered enum comparisons.
/// </summary>
public static class MovementForm
{
    /// <summary>The WasteReason and note fields belong to a <see cref="MovementType.Wasted"/> movement only.</summary>
    public static bool ShowsWasteFields(MovementType type) => type == MovementType.Wasted;

    /// <summary>A free-text reason is required for an <see cref="MovementType.Adjusted"/> movement.</summary>
    public static bool RequiresReason(MovementType type) => type == MovementType.Adjusted;

    /// <summary>Only an <see cref="MovementType.Adjusted"/> movement may be posted with a negative quantity.</summary>
    public static bool AllowsNegativeQuantity(MovementType type) => type == MovementType.Adjusted;
}
