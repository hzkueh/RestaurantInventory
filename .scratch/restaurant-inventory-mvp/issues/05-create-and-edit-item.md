# 05 — Create / edit InventoryItem

**What to build:** The Manager can start tracking a new consumable by creating an InventoryItem, and keep item metadata current by editing it — while the UnitOfMeasure stays fixed after creation so historical StockMovements remain meaningful in one unit.

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] Create an InventoryItem with a name, `UnitOfMeasure`, `UnitCost`, and `ReorderLevel`; it then appears in the items list.
- [ ] `UnitOfMeasure` is chosen from the fixed set (kg, g, L, ml, each).
- [ ] Edit an item's name, `UnitCost`, and `ReorderLevel`.
- [ ] `UnitOfMeasure` cannot be changed after creation.
- [ ] Covers user stories 5–8.
