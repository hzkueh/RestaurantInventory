# Restaurant Inventory

A single-restaurant inventory system. A manager tracks the consumable stock the
kitchen uses, watching for shortages and recording waste. The system exists to
showcase a clean .NET backend.

## Language

**Manager**:
The single person who runs the restaurant's inventory — the only user of the system.
Signs in with a seeded account and is the actor behind every StockMovement and reorder
decision. There is no other role or user; multi-user is deliberately out of scope.
_Avoid_: User, admin, staff, operator, account

**InventoryItem**:
A consumable the kitchen holds in stock — a raw ingredient or supply that gets
used up (flour, tomatoes, oil). Has one UnitOfMeasure, a ReorderLevel, and a
QuantityOnHand derived from its StockMovements.
_Avoid_: Product, ingredient, item, material, SKU

**StockMovement**:
An append-only record of a single change to an InventoryItem's stock. Its Type is
one of **Received** (delivery in), **Wasted** (loss out), or **Adjusted** (a +/−
physical stock-count correction). Carries quantity, timestamp, and reason. The
source of truth for quantity — never edited or deleted; mistakes are corrected by
posting a compensating movement. See [ADR-0001](docs/adr/0001-stockmovement-ledger.md).
_Avoid_: Transaction, entry, log

**QuantityOnHand**:
The stock currently held for an InventoryItem — the sum of its StockMovements,
cached on the item for fast reads.
_Avoid_: Stock, count, balance, level

**ReorderLevel**:
A per-item, manager-set threshold quantity. When QuantityOnHand falls to or below
it, the item is in Shortage.
_Avoid_: Par level, minimum, threshold

**Shortage**:
The *computed* condition `QuantityOnHand <= ReorderLevel`. A query, not a stored
status or a manual flag.
_Avoid_: Out of stock, low stock, deficit

**Waste**:
Stock recorded as leaving inventory without serving customers, via a `Wasted`
StockMovement. Every Waste carries a required WasteReason —
`Spoiled | Expired | Spilled | Overproduction | Other` — plus an optional note.
_Avoid_: Loss, spoilage, scrap

**UnitOfMeasure**:
The single unit an InventoryItem is counted in (kg, g, L, ml, each). Fixed per
item; the system does no conversion between units.
_Avoid_: UOM, measure, unit type

## Out of scope (deliberately)

- **Durable goods / equipment** (blenders, plates). "Broken" as a concept was
  considered and dropped — durables are a different model. Possible later stretch.
- **Multi-restaurant / multi-location.** One restaurant, one inventory.
- **Recipes / menu items / stock deduction on sale.** Not modelled.
