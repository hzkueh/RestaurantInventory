namespace RestaurantInventory.Core.Domain;

/// <summary>
/// Thrown when creating or editing an <see cref="InventoryItem"/> would violate a metadata
/// invariant (blank name, negative unit cost or reorder level). Parallels
/// <see cref="InvalidMovementException"/> for the ledger: the change is rejected before anything
/// is persisted, so an item never enters an invalid state.
/// </summary>
public sealed class InvalidItemException : Exception
{
    public InvalidItemException(string message) : base(message)
    {
    }
}
