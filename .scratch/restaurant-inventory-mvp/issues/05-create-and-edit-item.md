# 05 — Create / edit InventoryItem

**What to build:** The Manager can start tracking a new consumable by creating an InventoryItem, and keep item metadata current by editing it — while the UnitOfMeasure stays fixed after creation so historical StockMovements remain meaningful in one unit.

**Blocked by:** 04.

**Status:** ready-for-agent

- [x] Create an InventoryItem with a name, `UnitOfMeasure`, `UnitCost`, and `ReorderLevel`; it then appears in the items list.
- [x] `UnitOfMeasure` is chosen from the fixed set (kg, g, L, ml, each).
- [x] Edit an item's name, `UnitCost`, and `ReorderLevel`.
- [x] `UnitOfMeasure` cannot be changed after creation.
- [x] Covers user stories 5–8.

## Comments

Implemented on branch `main`. Create/edit added to the `InventoryService` seam (`CreateItemAsync`,
`UpdateItemAsync` — the latter deliberately takes no `UnitOfMeasure`), with the three editable
fields funnelled through a validating `InventoryItem.UpdateDetails`; the unit has no mutator, so it
is fixed structurally. New Blazor SSR pages `/items/new` and `/items/{id}/edit`, entry points on the
list and detail pages. Covered by domain + SQLite integration tests and verified in-browser.
