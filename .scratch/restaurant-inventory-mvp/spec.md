# Spec: Restaurant Inventory MVP

Status: complete

_All 10 implementation tickets delivered and merged to `main` (tickets 01–10). User stories 1–32 covered._

_Vocabulary follows [CONTEXT.md](../../CONTEXT.md). Respects [ADR-0001](../../docs/adr/0001-stockmovement-ledger.md) (StockMovement ledger) and [ADR-0002](../../docs/adr/0002-ai-insight-provider.md) (AI insight behind a seam)._

## Problem Statement

A restaurant Manager has no reliable way to know what consumable stock the kitchen
currently holds, which InventoryItems have run low, or how much money is being lost
to Waste. Stock is tracked in someone's head or on paper: quantities drift from
reality, Shortages are noticed only when a line cook reaches for an empty tub, and
there is no honest record of *why* stock left inventory. The Manager cannot make
informed reordering decisions or see where money is leaking.

## Solution

A single-restaurant inventory system for one Manager. The Manager keeps a list of
InventoryItems, each with a UnitOfMeasure, a UnitCost, and a manager-set
ReorderLevel. Every change to stock is posted as an append-only StockMovement
(Received, Wasted, or Adjusted); QuantityOnHand is the sum of an item's movements,
cached for fast reads. The system computes Shortage (`QuantityOnHand <= ReorderLevel`)
rather than storing a flag, surfaces every Waste with a required WasteReason and its
money value, and offers an on-demand AI narrative summarising the current state. The
result is a clean, auditable picture of what the kitchen holds, what is short, and
what waste is costing — the portfolio goal being to showcase a clean .NET backend
behind a thin-but-real UI.

## User Stories

1. As a Manager, I want to sign in with a seeded account, so that only I can view and change the restaurant's inventory.
2. As a Manager, I want to be kept signed in across page loads via a cookie, so that I don't re-authenticate on every action.
3. As a Manager, I want to sign out, so that I can leave the terminal without exposing the inventory.
4. As a Manager, I want to be redirected to login when I hit any page while unauthenticated, so that no inventory data is reachable without signing in.
5. As a Manager, I want to create an InventoryItem with a name, UnitOfMeasure, UnitCost, and ReorderLevel, so that I can start tracking a new consumable.
6. As a Manager, I want to pick the UnitOfMeasure from the fixed set (kg, g, L, ml, each), so that every item is counted consistently and the system never has to convert units.
7. As a Manager, I want to edit an InventoryItem's name, UnitCost, and ReorderLevel, so that I can keep item metadata current as prices and thresholds change.
8. As a Manager, I want an InventoryItem's UnitOfMeasure to be fixed after creation, so that its historical StockMovements stay meaningful in one unit.
9. As a Manager, I want to see a list of all InventoryItems with their current QuantityOnHand, so that I can see everything the kitchen holds at a glance.
10. As a Manager, I want each item in the list to show a Shortage badge when it is in Shortage, so that I can spot what needs reordering without doing math.
11. As a Manager, I want to filter the items list to only those in Shortage, so that I can focus on what needs action.
12. As a Manager, I want to open an InventoryItem's detail page, so that I can see its full StockMovement history.
13. As a Manager, I want the detail page to show QuantityOnHand, ReorderLevel, UnitOfMeasure, and UnitCost together, so that I understand the item's status in one place.
14. As a Manager, I want the movement history listed newest-first with type, quantity, timestamp, and reason, so that I can audit exactly how the current quantity was reached.
15. As a Manager, I want to post a Received StockMovement with a quantity, so that a delivery increases QuantityOnHand.
16. As a Manager, I want to post a Wasted StockMovement with a quantity and a required WasteReason, so that spoilage and losses are recorded honestly and QuantityOnHand decreases.
17. As a Manager, I want to attach an optional note to a Waste, so that I can capture context the WasteReason alone doesn't carry.
18. As a Manager, I want to post an Adjusted StockMovement (positive or negative) with a reason, so that I can reconcile the system to a physical stock count.
19. As a Manager, I want each StockMovement to carry a timestamp automatically, so that the history is chronologically accurate without manual entry.
20. As a Manager, I want QuantityOnHand to update immediately after I post a movement, so that the list and detail views always reflect the latest stock.
21. As a Manager, I want the system to reject a movement that would be nonsensical (e.g. non-positive quantity where a positive is required), so that the ledger stays trustworthy.
22. As a Manager, I want to correct a mistaken movement by posting a compensating movement rather than editing history, so that the ledger remains append-only and auditable.
23. As a Manager, I want a Waste to be valued in money using the item's UnitCost, so that I can see the financial cost of what was thrown away.
24. As a Manager, I want a dashboard showing the count of InventoryItems currently in Shortage, so that I have a single headline number for reorder urgency.
25. As a Manager, I want the dashboard to show total Waste broken down by WasteReason in money terms, so that I can see which kind of loss is costing the most.
26. As a Manager, I want the waste breakdown to cover a sensible recent window, so that the figures reflect current operations rather than all history.
27. As a Manager, I want an on-demand AI summary page that narrates current inventory state (Shortages, recent movements, Waste totals), so that I get a quick plain-language briefing without reading every screen.
28. As a Manager, I want the AI summary to be generated only when I ask for it, so that I don't incur unnecessary calls or waiting on every visit.
29. As a Manager, I want the app to keep working and tell me the AI summary is unavailable when no AI key is configured, so that a missing key never breaks the rest of the system.
30. As a Manager, I want realistic seeded data on first run, so that every screen is populated and demonstrable without manual setup.
31. As a reviewer, I want to clone the repo and run it in about 30 seconds, so that I can evaluate the backend without a setup ordeal.
32. As a reviewer, I want a README covering run steps, architecture notes, and screenshots, so that I understand the design before reading code.

## Implementation Decisions

**Scope:** One spec covering the whole MVP as described in [DESIGN.md](../../docs/DESIGN.md). The Cut line in DESIGN (suppliers/POs, shortage email alerts, durables model, CSV/PDF export, consumption tracking, multi-user/roles) is out of scope — see below.

**Stack:** ASP.NET Core + Blazor Server, one .NET solution, all C#. EF Core + SQLite with migrations and seeded data (clone-and-run in ~30s). ASP.NET Core Identity with one seeded Manager and cookie auth. xUnit for tests.

**Primary seam — the application-level inventory service.** A single application service owns all ledger behaviour and is the highest seam below the Blazor UI:
- Post a StockMovement (Received / Wasted / Adjusted) for an InventoryItem, with validation.
- Maintain the cached `QuantityOnHand` on the item on every movement (per ADR-0001: derived-and-snapshotted read model, movements remain source of truth).
- Query which InventoryItems are in Shortage (`QuantityOnHand <= ReorderLevel`) — computed, never a stored flag.
- Aggregate Waste by WasteReason valued in money via `UnitCost`, over a recent window.
This service is the single seam for the core; Blazor pages are thin callers of it. It is the ideal one-seam target for the ledger math.

**Second seam — `IInventoryInsightService`** (per ADR-0002). Application-owned interface; the AI summary page depends only on it. `GeminiInsightService` implements it using a typed `HttpClient` via `IHttpClientFactory` calling `gemini-3.1-flash-lite` (free tier), with no third-party AI SDK. The model ID and API key are config; the key lives in a git-ignored `.env` with a committed `.env.example`. When no key is configured the implementation degrades gracefully to an "unavailable" state and the rest of the app is unaffected. Swapping providers is a single new implementation of the interface.

**Domain model:**
- **InventoryItem** — name, `UnitOfMeasure` (kg/g/L/ml/each, fixed after creation, no conversion), `UnitCost`, manager-set `ReorderLevel`, cached `QuantityOnHand`.
- **StockMovement** — append-only. Fields: item reference, `Type` (`Received | Wasted | Adjusted`), quantity, timestamp, reason. A `Wasted` movement additionally carries a required `WasteReason` (`Spoiled | Expired | Spilled | Overproduction | Other`) and an optional note. Never edited or deleted.
- **Shortage** — computed query, not persisted.
- **Waste** — a `Wasted` movement; its money value is `quantity × UnitCost`.

**Movement validation rules:** Received quantity must be positive; Wasted quantity must be positive (it decreases stock) and requires a WasteReason; Adjusted may be positive or negative and requires a reason. QuantityOnHand is recomputed/updated inside the same operation that appends the movement so reads and the cache never diverge.

**Persistence:** EF Core maps InventoryItem and StockMovement; StockMovement rows are insert-only from the application's perspective. Enums (`UnitOfMeasure`, movement `Type`, `WasteReason`) are first-class. SQLite file DB for the app; migrations applied and seed data inserted on startup.

**Screens (MVP):** Login · Items list (Shortage badges + Shortage filter) · Item detail with movement history · Record movement (type-aware form: WasteReason + note appear for Wasted, sign allowed for Adjusted) · Dashboard (Shortage count + Waste-by-reason in $) · AI summary (on-demand narrative).

**Auth:** ASP.NET Core Identity, one seeded Manager, cookie auth; all inventory pages require authentication and redirect to Login otherwise.

## Testing Decisions

**What makes a good test here:** exercises externally observable behaviour through a seam, not internal implementation. Assert on outcomes a Manager would care about — resulting QuantityOnHand after a sequence of movements, whether an item is reported in Shortage, the money value of Waste by reason, validation rejections — never on private fields or EF internals. Tests must survive a refactor of *how* the cache or queries are implemented.

**Primary target — the application-level inventory service (the one seam):**
- Unit tests on the ledger math: post sequences of Received/Wasted/Adjusted and assert resulting QuantityOnHand; a compensating movement restores the intended quantity without any history being edited; Shortage boundary at exactly `QuantityOnHand == ReorderLevel` (in Shortage) vs just above; validation rejects non-positive Received/Wasted quantities and Waste missing a WasteReason; Adjusted accepts negative quantities.
- Waste aggregation: Waste-by-WasteReason totals in money via `UnitCost`, and the recent-window boundary.
- SQLite in-memory integration tests: the same behaviours through EF Core against an in-memory SQLite database, confirming the cached QuantityOnHand persists and reloads correctly and that movements are insert-only.

**Second target — `IInventoryInsightService`:** the AI summary page is tested against a mocked `IInventoryInsightService` (per ADR-0002, testable without a network), including the graceful "unavailable" path when the provider reports no key. `GeminiInsightService`'s HTTP wiring is not exercised against the live Gemini endpoint in the test suite.

**Prior art:** none yet — greenfield repo. These become the reference tests: keep ledger unit tests fast and pure, and keep SQLite in-memory integration tests as the pattern for any future data-layer test.

## Out of Scope

Per the DESIGN Cut line and CONTEXT "Out of scope":
- Suppliers, purchase orders, and reordering workflows (only the Shortage signal is in scope).
- Shortage **email alerts** (candidate stretch item only).
- Durable goods / equipment and any "broken" model — a different domain.
- Multi-restaurant / multi-location; multi-user or role hierarchy beyond the single seeded Manager.
- Recipes, menu items, and automatic stock deduction on sale / consumption tracking.
- CSV / PDF export.
- Unit **conversion** between UnitOfMeasures.
- Editing or deleting StockMovement history (corrections are compensating movements only).

## Further Notes

- The append-only ledger (ADR-0001) is the domain point of the system — waste and shortage are first-class and auditable. Do not reintroduce a mutable authoritative quantity field; QuantityOnHand stays a derived-and-snapshotted cache.
- The AI feature is deliberately optional and isolated behind its seam; it must never be on the critical path of core inventory operations.
- The Day-6 README is treated as a first-class deliverable — it is the first thing a portfolio reviewer reads.
- If core work finishes inside the one-week budget, pull exactly one stretch item; DESIGN recommends the shortage email alert.
- Implementation issues for this spec should be created one-file-per-ticket under `.scratch/restaurant-inventory-mvp/issues/`, numbered from `01`, per the issue-tracker conventions.
