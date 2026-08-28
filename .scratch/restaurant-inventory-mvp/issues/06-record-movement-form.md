# 06 — Record-movement form (type-aware)

**What to build:** The Manager records stock changes from the UI — a delivery, a waste, or a physical-count correction — through the inventory service. The form adapts to the movement type, refuses nonsensical input, and the item's QuantityOnHand and history reflect the change immediately. Corrections are made by posting a compensating movement, never by editing history.

**Blocked by:** 04.

**Status:** ready-for-agent

- [x] Post a `Received` movement with a quantity; `QuantityOnHand` increases.
- [x] Post a `Wasted` movement with a quantity and a required `WasteReason` plus an optional note; `QuantityOnHand` decreases.
- [x] Post an `Adjusted` movement (positive or negative) with a reason.
- [x] Type-aware form: `WasteReason` + note fields appear only for `Wasted`; a sign is allowed for `Adjusted`.
- [x] Each movement is timestamped automatically.
- [x] Nonsensical movements (e.g. non-positive quantity where a positive is required, or a Waste missing its reason) are rejected and the error is surfaced to the Manager.
- [x] After posting, the list and detail views reflect the latest `QuantityOnHand` immediately.
- [x] A recorded Waste is shown valued in money via the item's `UnitCost`.
- [x] Covers user stories 15–23.
