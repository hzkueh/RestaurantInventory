# 01 — Solution scaffold + domain model + persistence foundation

**What to build:** A reviewer can clone the repo, run it, and the app boots and creates its SQLite database with the correct schema — no manual migration step. The domain spine (InventoryItem, StockMovement, and the three enums) exists as first-class EF Core mappings, and the solution builds with an empty xUnit test project ready for later tickets.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [x] One .NET solution containing an ASP.NET Core + Blazor Server app project and an xUnit test project; `dotnet build` succeeds.
- [x] `InventoryItem` entity: name, `UnitOfMeasure`, `UnitCost`, manager-set `ReorderLevel`, cached `QuantityOnHand`.
- [x] `StockMovement` entity: item reference, `Type`, quantity, timestamp, reason; a `Wasted` movement additionally carries `WasteReason` and an optional note.
- [x] Enums are first-class in the model and persistence: `UnitOfMeasure` (kg/g/L/ml/each), movement `Type` (Received/Wasted/Adjusted), `WasteReason` (Spoiled/Expired/Spilled/Overproduction/Other).
- [x] EF Core + SQLite `DbContext` maps both entities; an initial migration exists.
- [x] Migrations are applied automatically on startup so a fresh clone-and-run produces the schema with no manual step.
- [x] `QuantityOnHand` remains a cached/derived field per [ADR-0001](../../../docs/adr/0001-stockmovement-ledger.md) — no mutable authoritative quantity is introduced.

## Comments

**2026-08-26 — implemented.** Solution `RestaurantInventory.sln` with three projects:
`src/RestaurantInventory.Core` (domain + EF Core persistence), `src/RestaurantInventory.Web`
(Blazor Web App, Server interactivity — the .NET 10 successor to the `blazorserver`
template), and `tests/RestaurantInventory.Tests` (xUnit). Domain lives in Core so the
Web UI and tests are thin callers.

- Enums (`UnitOfMeasure`, `MovementType`, `WasteReason`) persist by name (TEXT) for a
  readable, stable schema. Movement `Type` is named `MovementType` in code to avoid
  colliding with `System.Type`.
- `QuantityOnHand` has an `internal set` — updatable only from within Core (the ledger
  service in ticket 02), never an externally-editable authoritative field (ADR-0001).
- Initial migration `InitialCreate` generated via a design-time factory in Core;
  `db.Database.Migrate()` runs on startup. Verified: fresh boot creates
  `restaurantinventory.db` with `InventoryItems` + `StockMovements` tables and serves HTTP 200.
- `dotnet build` and `dotnet test` both green.

**Code-review follow-ups applied:** `StockMovement.Reason` made nullable at the schema
(the "required for Adjusted" rule belongs in the ticket-02 service, not the DB); no-op
template test replaced with two real scaffold smoke tests.

**Deferred to ticket 02:** `Timestamp` is `DateTimeOffset`, stored by SQLite as TEXT
including the offset. To keep the `(InventoryItemId, Timestamp)` index and newest-first
history correct, the inventory service must always stamp movements in **UTC** so string
ordering matches instant ordering.
