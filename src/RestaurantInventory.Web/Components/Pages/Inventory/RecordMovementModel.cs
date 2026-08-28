using System.ComponentModel.DataAnnotations;
using RestaurantInventory.Core.Domain;

namespace RestaurantInventory.Web.Components.Pages.Inventory;

/// <summary>
/// Form-bound input for posting a <see cref="StockMovement"/> through the inventory service
/// (ticket 06). The fields cover every movement type; which of the optional ones apply is decided
/// by <see cref="MovementForm"/> and by <see cref="Validate"/> here.
/// <para>
/// As with <see cref="ItemFormModel"/>, this validation exists only to give the Manager inline,
/// type-aware feedback up front. The <see cref="InventoryItem"/> aggregate remains the authority and
/// re-checks the same invariants on every seam call, so the two are kept deliberately in step:
/// Received/Wasted quantities must be positive, a Wasted movement requires a WasteReason, and an
/// Adjusted movement requires a reason and a non-zero (possibly negative) quantity.
/// </para>
/// </summary>
public sealed class RecordMovementModel : IValidatableObject
{
    /// <summary>Longest a free-text note or reason may be.</summary>
    public const int MaxTextLength = 500;

    [Required]
    public MovementType Type { get; set; } = MovementType.Received;

    public decimal Quantity { get; set; }

    /// <summary>Set only for a <see cref="MovementType.Wasted"/> movement; required there.</summary>
    public WasteReason? WasteReason { get; set; }

    /// <summary>Optional free-text context for a Waste.</summary>
    public string? Note { get; set; }

    /// <summary>Free-text reason; required for an <see cref="MovementType.Adjusted"/> movement.</summary>
    public string? Reason { get; set; }

    // Length is checked inside Validate for the type that owns each free-text field, not with a
    // property-level [StringLength], so a value left over from another type (the form keeps the
    // fields' state as the Manager switches type) cannot fail validation while its input — and so
    // its error message — is hidden, which would block the post with no visible reason.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (Type)
        {
            case MovementType.Received:
                if (Quantity <= 0)
                    yield return Error("Received quantity must be greater than zero.", nameof(Quantity));
                break;

            case MovementType.Wasted:
                if (Quantity <= 0)
                    yield return Error("Wasted quantity must be greater than zero.", nameof(Quantity));
                if (WasteReason is null)
                    yield return Error("Choose a waste reason.", nameof(WasteReason));
                if (Note?.Length > MaxTextLength)
                    yield return Error($"Note must be {MaxTextLength} characters or fewer.", nameof(Note));
                break;

            case MovementType.Adjusted:
                if (Quantity == 0)
                    yield return Error("An adjustment must be a non-zero quantity (use a minus sign to decrease).", nameof(Quantity));
                if (string.IsNullOrWhiteSpace(Reason))
                    yield return Error("Enter a reason for the adjustment.", nameof(Reason));
                else if (Reason.Length > MaxTextLength)
                    yield return Error($"Reason must be {MaxTextLength} characters or fewer.", nameof(Reason));
                break;
        }
    }

    private static ValidationResult Error(string message, string member) => new(message, new[] { member });
}
