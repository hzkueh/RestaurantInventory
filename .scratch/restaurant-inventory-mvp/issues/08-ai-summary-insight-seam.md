# 08 — AI summary: `IInventoryInsightService` + `GeminiInsightService` + summary page

**What to build:** On demand, the Manager gets a plain-language briefing of current inventory state (Shortages, recent movements, Waste totals) from a single LLM call — generated only when asked. If no AI key is configured the page says the summary is unavailable and the rest of the app keeps working. The provider is hidden behind an application-owned seam so swapping it is a single new implementation.

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [x] Application-owned `IInventoryInsightService` interface; the summary page depends only on it (per [ADR-0002](../../../docs/adr/0002-ai-insight-provider.md)).
- [x] `GeminiInsightService` implements it using a typed `HttpClient` via `IHttpClientFactory` calling `gemini-3.1-flash-lite`, with no third-party AI SDK.
- [x] Model ID and API key come from config; the key lives in a git-ignored `.env` with a committed `.env.example`; `.gitignore` excludes `.env`.
- [x] Summary page generates the narrative only when the Manager asks for it — not on every visit.
- [x] With no key configured, the page degrades gracefully to an "unavailable" state and the rest of the app is unaffected — the AI feature is never on the critical path of core inventory operations.
- [x] Summary page tested against a mocked `IInventoryInsightService`, including the graceful "unavailable" path; the live Gemini endpoint is not exercised in the test suite.
- [x] Covers user stories 27–29.

## Comments

**2026-08-29 — Complete.** All acceptance criteria implemented and verified. The summary page
depends only on `IInventoryInsightService`; `GeminiInsightService` calls `gemini-3.1-flash-lite`
via a typed `HttpClient` (no AI SDK). With no key the page shows an "unavailable" state (unit-tested,
network-free); with the key in the git-ignored `.env` a live briefing was generated end to end. The
briefing renders as a bold headline plus three labelled groups (Shortages first, then recent
movements, then Waste) with currency-formatted money. Tests: 121 passing.
