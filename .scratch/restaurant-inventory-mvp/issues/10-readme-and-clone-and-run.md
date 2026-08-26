# 10 — README + clone-and-run polish

**What to build:** A reviewer can clone the repo and have it running in about 30 seconds, and can read a README that explains the run steps, the architecture, and shows screenshots — so they understand the design before reading any code. The README is a first-class portfolio deliverable.

**Blocked by:** 04, 05, 06, 07, 08, 09.

**Status:** ready-for-agent

- [ ] Verify the clone-and-run experience: a fresh clone builds, migrates, seeds, and serves in ~30s with documented commands.
- [ ] README covers run steps (including the optional `.env` / AI key and that the app works without it).
- [ ] README covers architecture notes: the StockMovement ledger + derived `QuantityOnHand` ([ADR-0001](../../../docs/adr/0001-stockmovement-ledger.md)), the inventory service seam, and the `IInventoryInsightService` seam ([ADR-0002](../../../docs/adr/0002-ai-insight-provider.md)).
- [ ] README includes screenshots of the main screens (items list, item detail, record movement, dashboard, AI summary).
- [ ] Covers user stories 31–32.
