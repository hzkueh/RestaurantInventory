# 02 — Inventory service: the ledger seam + tests

**What to build:** The single application-level inventory service that owns all ledger behaviour, exercised entirely through tests. Posting a movement validates it, appends it, and updates the cached QuantityOnHand in one operation; the service answers which items are in Shortage and totals Waste by reason in money. This is the core of the system and is demoable through a green test suite before any UI exists.

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] Post a `Received` / `Wasted` / `Adjusted` StockMovement for an InventoryItem, appending to the ledger and updating cached `QuantityOnHand` inside the same operation so reads and cache never diverge.
- [ ] Validation: `Received` quantity must be positive; `Wasted` quantity must be positive and requires a `WasteReason`; `Adjusted` may be positive or negative and requires a reason. Invalid movements are rejected and not persisted.
- [ ] Shortage query returns items where `QuantityOnHand <= ReorderLevel` — computed, never a stored flag.
- [ ] Waste aggregation: totals by `WasteReason` valued in money via `UnitCost`, over a recent window.
- [ ] Unit tests (fast, pure) on the ledger math: sequences of movements resolve to the expected `QuantityOnHand`; a compensating movement restores the intended quantity with no history edited; Shortage boundary at exactly `QuantityOnHand == ReorderLevel` (in Shortage) vs just above; validation rejections; `Adjusted` accepts negatives.
- [ ] Waste-aggregation tests: by-reason money totals and the recent-window boundary.
- [ ] SQLite in-memory integration tests: the same behaviours through EF Core, confirming cached `QuantityOnHand` persists and reloads correctly and that movements are insert-only.
- [ ] Tests assert on externally observable outcomes, not private fields or EF internals.
