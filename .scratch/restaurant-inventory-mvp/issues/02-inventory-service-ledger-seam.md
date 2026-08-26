# 02 — Inventory service: the ledger seam + tests

**What to build:** The single application-level inventory service that owns all ledger behaviour, exercised entirely through tests. Posting a movement validates it, appends it, and updates the cached QuantityOnHand in one operation; the service answers which items are in Shortage and totals Waste by reason in money. This is the core of the system and is demoable through a green test suite before any UI exists.

**Blocked by:** 01.

**Status:** done

- [x] Post a `Received` / `Wasted` / `Adjusted` StockMovement for an InventoryItem, appending to the ledger and updating cached `QuantityOnHand` inside the same operation so reads and cache never diverge.
- [x] Validation: `Received` quantity must be positive; `Wasted` quantity must be positive and requires a `WasteReason`; `Adjusted` may be positive or negative and requires a reason. Invalid movements are rejected and not persisted.
- [x] Shortage query returns items where `QuantityOnHand <= ReorderLevel` — computed, never a stored flag.
- [x] Waste aggregation: totals by `WasteReason` valued in money via `UnitCost`, over a recent window.
- [x] Unit tests (fast, pure) on the ledger math: sequences of movements resolve to the expected `QuantityOnHand`; a compensating movement restores the intended quantity with no history edited; Shortage boundary at exactly `QuantityOnHand == ReorderLevel` (in Shortage) vs just above; validation rejections; `Adjusted` accepts negatives.
- [x] Waste-aggregation tests: by-reason money totals and the recent-window boundary.
- [x] SQLite in-memory integration tests: the same behaviours through EF Core, confirming cached `QuantityOnHand` persists and reloads correctly and that movements are insert-only.
- [x] Tests assert on externally observable outcomes, not private fields or EF internals.

## Comments

**2026-08-26 — implemented.** The ledger seam lives in two layers:

- **Aggregate invariants** on `InventoryItem.PostMovement(...)` — validates, appends to the
  append-only ledger, and updates cached `QuantityOnHand` in one operation (throws
  `InvalidMovementException` and mutates nothing on invalid input). `IsInShortage` is a
  `[NotMapped]` computed property (`QuantityOnHand <= ReorderLevel`), never a stored flag.
  This keeps the ledger maths pure and fast to unit-test with no database.
- **Application service** `InventoryService` (`src/RestaurantInventory.Core/Services/`) is the
  single seam the Blazor UI calls: `PostMovementAsync` (stamps the timestamp in **UTC** per the
  ticket-01 note, then persists), `GetShortagesAsync`, and `GetWasteByReasonAsync`. Registered
  in DI in `Program.cs`; takes a `TimeProvider` so the recent-window is deterministic in tests.

Shortage and Waste are evaluated in memory after loading (SQLite has no native decimal, so
server-side decimal compare/SUM is unreliable) — fine at single-restaurant scale. Waste money =
`quantity × UnitCost`, summed by reason and rounded to 2 dp, costliest first.

**Tests (27 green):** pure ledger maths (`Ledger/InventoryItemLedgerTests`), pure waste
aggregation incl. window boundary (`Ledger/WasteAggregationTests`), and SQLite in-memory
integration through EF Core (`Integration/InventoryServiceSqliteTests`) confirming the cached
quantity persists/reloads across contexts, movements are insert-only, and timestamps are UTC.

**Code-review follow-up applied:** dropped an unnecessary `.Include(i => i.Movements)` from the
write path (the append + cached-scalar update never read the history); tests stayed green.
