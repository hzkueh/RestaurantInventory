# 09 — Realistic seed data

**What to build:** On first run the app is populated with realistic InventoryItems and a history of StockMovements, so every screen — items list (with some items in Shortage), item detail/history, dashboard (shortage count + waste-by-reason), and the AI summary — is demonstrable without any manual setup.

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [ ] Realistic InventoryItems are seeded with varied `UnitOfMeasure`, `UnitCost`, and `ReorderLevel`.
- [ ] Seeded StockMovements (Received / Wasted / Adjusted, including Waste with reasons) produce sensible `QuantityOnHand` values, with at least some items landing in Shortage.
- [ ] Waste data spans the recent window so the dashboard breakdown is non-empty and demonstrable.
- [ ] Seeding runs on first run with no manual step and is idempotent (does not duplicate on subsequent runs).
- [ ] Seed movements are posted such that cached `QuantityOnHand` matches the ledger (no divergence introduced by seeding).
- [ ] Covers user story 30.
