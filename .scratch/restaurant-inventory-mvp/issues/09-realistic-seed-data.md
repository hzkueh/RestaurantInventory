# 09 — Realistic seed data

**What to build:** On first run the app is populated with realistic InventoryItems and a history of StockMovements, so every screen — items list (with some items in Shortage), item detail/history, dashboard (shortage count + waste-by-reason), and the AI summary — is demonstrable without any manual setup.

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [x] Realistic InventoryItems are seeded with varied `UnitOfMeasure`, `UnitCost`, and `ReorderLevel`.
- [x] Seeded StockMovements (Received / Wasted / Adjusted, including Waste with reasons) produce sensible `QuantityOnHand` values, with at least some items landing in Shortage.
- [x] Waste data spans the recent window so the dashboard breakdown is non-empty and demonstrable.
- [x] Seeding runs on first run with no manual step and is idempotent (does not duplicate on subsequent runs).
- [x] Seed movements are posted such that cached `QuantityOnHand` matches the ledger (no divergence introduced by seeding).
- [x] Covers user story 30.

## Comments

**2026-08-31 — Complete.** All acceptance criteria implemented and verified; merged via hzkueh/RestaurantInventory#6.

Implemented `InventorySeeder` (`src/RestaurantInventory.Core/Persistence/InventorySeeder.cs`), wired into
`Program.cs` startup after migrations (idempotent — skips once any InventoryItem exists). Seeds 9 items
across Kg/G/L/Each with varied cost and reorder levels; movements are replayed through the
`InventoryItem` aggregate at backdated timestamps so the cached `QuantityOnHand` is maintained by the
same code the live app uses (verified: zero cache/ledger divergence). Three items land in Shortage
(Eggs at the exact boundary, Saffron, Whole Milk); Waste spans all five WasteReasons within the recent
window. Backdating derives from the injected `TimeProvider`, so the window stays honest under a test
clock. Covered by `InventorySeederSqliteTests` (5 integration tests over real in-memory SQLite);
verified end-to-end against a fresh file DB on real startup.
