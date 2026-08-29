# 07 — Dashboard: shortage count + waste-by-reason in $

**What to build:** The Manager opens a dashboard and gets a single headline number for reorder urgency and a money-terms breakdown of what kind of waste is costing the most over a recent window — without reading every screen.

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [x] Dashboard shows the count of InventoryItems currently in Shortage.
- [x] Dashboard shows total Waste broken down by `WasteReason` in money terms (via the service's aggregation).
- [x] The waste breakdown covers a sensible recent window rather than all history.
- [x] Page is behind auth and reachable from the navigation shell.
- [x] Covers user stories 24–26.
