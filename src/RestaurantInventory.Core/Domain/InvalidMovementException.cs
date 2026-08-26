namespace RestaurantInventory.Core.Domain;

/// <summary>
/// Thrown when a <see cref="StockMovement"/> violates a ledger invariant (bad quantity,
/// missing required reason, etc.). The offending movement is never appended or persisted,
/// so the ledger and the cached <see cref="InventoryItem.QuantityOnHand"/> stay trustworthy.
/// </summary>
public sealed class InvalidMovementException : Exception
{
    public InvalidMovementException(string message) : base(message)
    {
    }
}
