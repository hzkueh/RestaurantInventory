# Quantity on hand is derived from an append-only StockMovement ledger

An InventoryItem's quantity is never stored as an authoritative editable field.
Instead every change to stock is recorded as an append-only **StockMovement**
(Received, Wasted, Adjusted, each with quantity, timestamp, and reason), and the
quantity on hand is the sum of those movements. We chose this over a simple
mutable `QuantityOnHand` field because it makes waste and shortage first-class,
auditable, and reportable — the domain point of the whole system — at the cost of
slightly more write logic.

## Consequences

- A `QuantityOnHand` value is cached on InventoryItem and updated on every
  movement, so reads stay fast. This is a derived-and-snapshotted read model, not
  pure event sourcing — movements remain the source of truth.
- Correcting a mistake means posting a compensating movement, never editing or
  deleting history.
