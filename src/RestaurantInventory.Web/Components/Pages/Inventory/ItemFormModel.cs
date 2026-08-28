using System.ComponentModel.DataAnnotations;
using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>
/// Form-bound metadata for creating or editing an <see cref="InventoryItem"/>. Mirrors the item's
/// editable fields plus the create-only <see cref="UnitOfMeasure"/>. The annotations exist only to
/// give the Manager inline validation up front; the <see cref="InventoryItem"/> aggregate remains
/// the authority and re-checks the same rules (blank/over-length name, negative cost or level) on
/// every seam call. They are kept deliberately in step with it — the name cap reuses the domain's
/// <see cref="InventoryItem.MaxNameLength"/>, and the ranges are minimum-only so the form never
/// rejects a value the domain would accept. The double lower bound forces the numeric Range
/// overload so decimals validate without the int-conversion pitfall.
/// </summary>
public sealed class ItemFormModel
{
    [Required(ErrorMessage = "Enter an item name.")]
    [StringLength(InventoryItem.MaxNameLength, ErrorMessage = "Name must be 200 characters or fewer.")]
    public string Name { get; set; } = "";

    [Required]
    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Kg;

    [Range(0d, double.MaxValue, ErrorMessage = "Unit cost cannot be negative.")]
    public decimal UnitCost { get; set; }

    [Range(0d, double.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
    public decimal ReorderLevel { get; set; }
}
