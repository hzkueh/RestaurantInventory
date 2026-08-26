# 06 — Record-movement form (type-aware)

**What to build:** The Manager records stock changes from the UI — a delivery, a waste, or a physical-count correction — through the inventory service. The form adapts to the movement type, refuses nonsensical input, and the item's QuantityOnHand and history reflect the change immediately. Corrections are made by posting a compensating movement, never by editing history.

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] Post a `Received` movement with a quantity; `QuantityOnHand` increases.
- [ ] Post a `Wasted` movement with a quantity and a required `WasteReason` plus an optional note; `QuantityOnHand` decreases.
- [ ] Post an `Adjusted` movement (positive or negative) with a reason.
- [ ] Type-aware form: `WasteReason` + note fields appear only for `Wasted`; a sign is allowed for `Adjusted`.
- [ ] Each movement is timestamped automatically.
- [ ] Nonsensical movements (e.g. non-positive quantity where a positive is required, or a Waste missing its reason) are rejected and the error is surfaced to the Manager.
- [ ] After posting, the list and detail views reflect the latest `QuantityOnHand` immediately.
- [ ] A recorded Waste is shown valued in money via the item's `UnitCost`.
- [ ] Covers user stories 15–23.
