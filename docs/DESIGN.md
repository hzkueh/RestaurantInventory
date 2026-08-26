# Restaurant Inventory — Design & Plan

A single-restaurant inventory system for one **Manager** to track **consumable**
stock, watch for **shortages**, and record **waste**. Its purpose as a portfolio
piece is to showcase a **clean .NET backend**, with a thin-but-real UI.

See also: [CONTEXT.md](../CONTEXT.md) (domain glossary) and [docs/adr/](adr/)
(architecture decisions).

## Domain model (the spine)

- **InventoryItem** — a consumable (flour, tomatoes, oil). Has a `UnitOfMeasure`
  (kg/g/L/ml/each, no conversion), a `UnitCost`, a manager-set `ReorderLevel`, and
  a cached `QuantityOnHand`.
- **StockMovement** — *append-only* ledger. Every stock change is a row:
  **Received**, **Wasted**, or **Adjusted**. Quantity on hand = sum of movements;
  mistakes are corrected by posting a compensating movement, never by editing
  history. ([ADR-0001](adr/0001-stockmovement-ledger.md))
- **Shortage** — *computed*: `QuantityOnHand <= ReorderLevel`. Never a stored flag.
- **Waste** — a `Wasted` movement carrying a required `WasteReason`
  (`Spoiled | Expired | Spilled | Overproduction | Other`) + optional note, valued
  in money via `UnitCost`.

## Stack

- **ASP.NET Core + Blazor Server**, one .NET solution, all C#.
- **EF Core + SQLite**, migrations, seeded data → clone-and-run in ~30s.
- **ASP.NET Core Identity**, one seeded **Manager**, cookie auth.
- **AI summary** → `IInventoryInsightService` seam, `GeminiInsightService` calling
  **`gemini-3.1-flash-lite`** (free tier) via a typed `HttpClient`
  (`IHttpClientFactory`); API key in a git-ignored `.env`, graceful degradation
  when absent. ([ADR-0002](adr/0002-ai-insight-provider.md))
- **xUnit** — focused tests on the ledger math + a few SQLite in-memory
  integration tests.

## Screens (the MVP)

Login · Items list (shortage badges + filter) · Item detail with movement history
· Record movement · Dashboard (shortage count + waste-by-reason in $) · AI summary
(on-demand narrative).

## Cut line (defends the one-week budget)

**OUT / stretch only:** suppliers & purchase orders · shortage email alerts ·
durables/"broken" model · CSV/PDF export · consumption tracking · multi-user/roles.

If core finishes early, pull **one** stretch item (recommended: shortage email
alert).

## 7-day plan

| Day | Focus |
|-----|-------|
| 1 | Solution scaffold · EF Core + SQLite · domain entities + enums · migrations · seed data · Identity wired |
| 2 | Ledger logic: post movements, maintain cached `QuantityOnHand`, shortage query, waste aggregation · unit tests on the math |
| 3 | Blazor + auth (login, seeded Manager) · Items list + Item detail/history |
| 4 | Record-movement form · Dashboard (shortages + waste-by-reason $) |
| 5 | AI feature: `IInventoryInsightService` + `GeminiInsightService` + summary page · `.env` / `.gitignore` / `.env.example` · graceful degradation |
| 6 | Integration tests · UI polish · README (run steps, architecture notes, screenshots) · realistic seed data |
| 7 | Buffer · one stretch item · record a demo · final cleanup |

The Day-6 README matters as much as any feature — it is the first thing a reviewer
reads.

## Decisions on record

- [ADR-0001 — StockMovement ledger](adr/0001-stockmovement-ledger.md)
- [ADR-0002 — AI insight provider (Gemini behind a seam)](adr/0002-ai-insight-provider.md)
