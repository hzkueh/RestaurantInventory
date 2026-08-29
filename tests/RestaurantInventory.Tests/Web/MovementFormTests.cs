using System.ComponentModel.DataAnnotations;
using RestaurantInventory.Core.Domain;
using RestaurantInventory.Web.Components.Pages.Inventory;

namespace RestaurantInventory.Tests.Web;

/// <summary>
/// The type-aware record-movement form logic (ticket 06). Two pure pieces are tested here so the
/// headline behaviour — the form adapting to the movement type and refusing nonsensical input —
/// is pinned by tests rather than only visible on screen:
/// <list type="bullet">
///   <item><see cref="MovementForm"/>: which fields apply to which <see cref="MovementType"/>.</item>
///   <item><see cref="RecordMovementModel"/>: its up-front validation, which mirrors the domain
///   invariants the <see cref="InventoryItem"/> aggregate enforces on every seam call.</item>
/// </list>
/// </summary>
public sealed class MovementFormTests
{
    // --- MovementForm: type-aware field applicability (acceptance criterion #4) ---

    [Fact]
    public void WasteFields_ApplyOnlyToWasted()
    {
        Assert.True(MovementForm.ShowsWasteFields(MovementType.Wasted));
        Assert.False(MovementForm.ShowsWasteFields(MovementType.Received));
        Assert.False(MovementForm.ShowsWasteFields(MovementType.Adjusted));
    }

    [Fact]
    public void FreeTextReason_AppliesOnlyToAdjusted()
    {
        Assert.True(MovementForm.RequiresReason(MovementType.Adjusted));
        Assert.False(MovementForm.RequiresReason(MovementType.Received));
        Assert.False(MovementForm.RequiresReason(MovementType.Wasted));
    }

    [Fact]
    public void NegativeQuantity_AllowedOnlyForAdjusted()
    {
        Assert.True(MovementForm.AllowsNegativeQuantity(MovementType.Adjusted));
        Assert.False(MovementForm.AllowsNegativeQuantity(MovementType.Received));
        Assert.False(MovementForm.AllowsNegativeQuantity(MovementType.Wasted));
    }

    // --- RecordMovementModel: up-front validation mirroring the domain (criterion #6) ---

    private static IReadOnlyList<ValidationResult> Validate(RecordMovementModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Received_RequiresPositiveQuantity()
    {
        Assert.NotEmpty(Validate(new RecordMovementModel { Type = MovementType.Received, Quantity = 0m }));
        Assert.NotEmpty(Validate(new RecordMovementModel { Type = MovementType.Received, Quantity = -5m }));
        Assert.Empty(Validate(new RecordMovementModel { Type = MovementType.Received, Quantity = 5m }));
    }

    [Fact]
    public void Wasted_RequiresPositiveQuantityAndReason()
    {
        // Missing WasteReason is rejected even with a good quantity.
        Assert.NotEmpty(Validate(new RecordMovementModel { Type = MovementType.Wasted, Quantity = 2m }));
        // Non-positive quantity is rejected even with a reason.
        Assert.NotEmpty(Validate(new RecordMovementModel
        {
            Type = MovementType.Wasted,
            Quantity = 0m,
            WasteReason = WasteReason.Spoiled,
        }));
        Assert.Empty(Validate(new RecordMovementModel
        {
            Type = MovementType.Wasted,
            Quantity = 2m,
            WasteReason = WasteReason.Spoiled,
        }));
    }

    [Fact]
    public void Adjusted_RequiresReasonAndNonZeroQuantity_AllowsNegative()
    {
        // Missing reason is rejected.
        Assert.NotEmpty(Validate(new RecordMovementModel { Type = MovementType.Adjusted, Quantity = 3m }));
        // Zero is rejected.
        Assert.NotEmpty(Validate(new RecordMovementModel
        {
            Type = MovementType.Adjusted,
            Quantity = 0m,
            Reason = "recount",
        }));
        // A negative adjustment with a reason is valid.
        Assert.Empty(Validate(new RecordMovementModel
        {
            Type = MovementType.Adjusted,
            Quantity = -3m,
            Reason = "recount",
        }));
    }

    [Fact]
    public void FreeText_LengthIsCheckedOnlyForTheTypeThatShowsTheField()
    {
        var tooLong = new string('x', RecordMovementModel.MaxTextLength + 1);

        // An over-length Note left over from a Wasted entry must not block a Received post: the Note
        // field (and its error message) is hidden for Received, so validating it there is a dead-end.
        Assert.Empty(Validate(new RecordMovementModel { Type = MovementType.Received, Quantity = 5m, Note = tooLong }));
        // Under Wasted, where the Note field is shown, the length is enforced.
        Assert.NotEmpty(Validate(new RecordMovementModel
        {
            Type = MovementType.Wasted,
            Quantity = 2m,
            WasteReason = WasteReason.Spoiled,
            Note = tooLong,
        }));

        // Likewise a leftover over-length Reason must not block a Received post, but is enforced under Adjusted.
        Assert.Empty(Validate(new RecordMovementModel { Type = MovementType.Received, Quantity = 5m, Reason = tooLong }));
        Assert.NotEmpty(Validate(new RecordMovementModel { Type = MovementType.Adjusted, Quantity = 1m, Reason = tooLong }));
    }
}
