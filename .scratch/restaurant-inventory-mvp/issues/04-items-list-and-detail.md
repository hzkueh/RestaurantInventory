# 04 — Items list + Item detail with movement history

**What to build:** The Manager sees every InventoryItem the kitchen holds with its current QuantityOnHand at a glance, spots what needs reordering via a Shortage badge, and can filter to only items in Shortage. Opening an item shows its full status in one place plus a complete, newest-first audit of how the current quantity was reached. Read-only.

**Blocked by:** 02, 03.

**Status:** ready-for-human

- [x] Items list shows all InventoryItems with their current `QuantityOnHand`.
- [x] Each row shows a Shortage badge when the item is in Shortage (via the service's Shortage query).
- [x] A filter narrows the list to only items in Shortage.
- [x] Item detail page shows `QuantityOnHand`, `ReorderLevel`, `UnitOfMeasure`, and `UnitCost` together.
- [x] Item detail lists the movement history newest-first with type, quantity, timestamp, and reason.
- [x] Pages are behind auth and reachable from the navigation shell.
- [x] Covers user stories 9–14.

## Comments

Implemented on branch `main`.

- **Seam.** Added two read methods to `InventoryService` (the one ledger seam): `GetItemsAsync(shortagesOnly)` backs the list and its Shortage filter; `GetItemDetailAsync(id)` loads one item with its movement history (null when absent). `GetShortagesAsync` now delegates to `GetItemsAsync(shortagesOnly: true)` so there is a single Shortage query path. Covered by `ItemQueriesSqliteTests` (5 tests) through in-memory SQLite.
- **UI.** `Components/Pages/Inventory/Items.razor` (`/items`) and `ItemDetail.razor` (`/items/{id:int}`), plus a shared `UnitDisplay` helper. Both are static SSR — the filter is a `?shortages=true` query-string toggle, no interactive circuit needed for a read-only screen. Movement history is sorted newest-first in the page (timestamp desc, id desc as a stable tiebreaker); the "Change" column shows the signed ledger delta.
- **Nav/auth.** An **Items** link added to the navigation shell; pages inherit the app-wide `[Authorize]` guard.
- **Verified** in the browser (seeded data): unauthenticated `/items` redirects to Login; list is name-ordered with correct quantities and Shortage badges (boundary `QoH == ReorderLevel` counts as Shortage); filter narrows to shortages; detail shows the four figures together and newest-first history with Received/Wasted/Adjusted signs, WasteReason + note; unknown id shows a clean "not found". Full suite: 37 passing.
