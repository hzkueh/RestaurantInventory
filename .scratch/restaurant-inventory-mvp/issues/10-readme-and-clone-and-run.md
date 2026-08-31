# 10 — README + clone-and-run polish

**What to build:** A reviewer can clone the repo and have it running in about 30 seconds, and can read a README that explains the run steps, the architecture, and shows screenshots — so they understand the design before reading any code. The README is a first-class portfolio deliverable.

**Blocked by:** 04, 05, 06, 07, 08, 09.

**Status:** ready-for-agent

- [x] Verify the clone-and-run experience: a fresh clone builds, migrates, seeds, and serves in ~30s with documented commands.
- [x] README covers run steps (including the optional `.env` / AI key and that the app works without it).
- [x] README covers architecture notes: the StockMovement ledger + derived `QuantityOnHand` ([ADR-0001](../../../docs/adr/0001-stockmovement-ledger.md)), the inventory service seam, and the `IInventoryInsightService` seam ([ADR-0002](../../../docs/adr/0002-ai-insight-provider.md)).
- [x] README includes screenshots of the main screens (items list, item detail, record movement, dashboard, AI summary).
- [x] Covers user stories 31–32.

## Comments

**2026-08-31 — Complete.** Wrote [`README.md`](../../../README.md) as the portfolio deliverable:
one-command quick start (`dotnet run --project src/RestaurantInventory.Web` → http://localhost:5066,
migrations + seed on startup), seeded sign-in credentials, and the optional `.env` / Gemini key with
the explicit note that the app runs fully without it. Architecture section covers the append-only
StockMovement ledger + derived-and-snapshotted `QuantityOnHand` (ADR-0001), the single `InventoryService`
seam, and the `IInventoryInsightService` seam (ADR-0002), plus stack, project layout, and testing.

Clone-and-run verified end-to-end against a fresh (deleted) SQLite file: clean build (~8s), migrations
applied and realistic data seeded on startup, served on :5066. Six screenshots captured via Playwright
(driving Edge) after a real login — items list, item detail, record-movement (Wasted state with live
value), dashboard, AI summary (generated live via Gemini), and sign-in — committed under
`docs/screenshots/`. Full suite green: 126/126.
