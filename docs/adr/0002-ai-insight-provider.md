# AI inventory summary uses Gemini free tier behind an IInventoryInsightService seam

The optional "AI summary" page generates a natural-language briefing of current
inventory state (shortages, recent movements, waste totals) with a single LLM
call. We call **Google Gemini** (`gemini-3.1-flash-lite`, free tier) rather than a
paid provider, to keep the portfolio zero-cost to run. The provider is hidden
behind an application-owned `IInventoryInsightService` interface; the Gemini
implementation makes its call with a typed `HttpClient` (`IHttpClientFactory`),
with no third-party AI SDK dependency.

## Consequences

- Swapping providers (Claude, OpenAI, etc.) is a single new implementation of the
  interface — the Blazor UI and services never reference Gemini.
- The feature is testable without a network by mocking the interface.
- The API key lives in a git-ignored `.env` (a committed `.env.example` documents
  it). If no key is configured, the AI page degrades gracefully to an
  "unavailable" message and the rest of the app is unaffected.
- Free-tier rate limits and the exact model string can change; the model ID is a
  single config value. Verified `gemini-3.1-flash-lite` on the free tier, Aug 2026.
