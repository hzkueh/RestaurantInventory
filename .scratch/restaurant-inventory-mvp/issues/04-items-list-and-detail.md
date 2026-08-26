# 04 — Items list + Item detail with movement history

**What to build:** The Manager sees every InventoryItem the kitchen holds with its current QuantityOnHand at a glance, spots what needs reordering via a Shortage badge, and can filter to only items in Shortage. Opening an item shows its full status in one place plus a complete, newest-first audit of how the current quantity was reached. Read-only.

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [ ] Items list shows all InventoryItems with their current `QuantityOnHand`.
- [ ] Each row shows a Shortage badge when the item is in Shortage (via the service's Shortage query).
- [ ] A filter narrows the list to only items in Shortage.
- [ ] Item detail page shows `QuantityOnHand`, `ReorderLevel`, `UnitOfMeasure`, and `UnitCost` together.
- [ ] Item detail lists the movement history newest-first with type, quantity, timestamp, and reason.
- [ ] Pages are behind auth and reachable from the navigation shell.
- [ ] Covers user stories 9–14.
