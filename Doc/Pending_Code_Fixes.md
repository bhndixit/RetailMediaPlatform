# Pending Code Fixes — To Apply Before Final Submission

Status: **Not yet applied to the repo.** This document tracks the outstanding code changes agreed on during review, so nothing gets lost before submission.

---

## 1. Basket-Counting Fix (Priority: High — correctness bug)

**Status:** ⬜ Not applied yet — code was shared earlier in this conversation but hasn't been pasted into the repo.

**Files to change:**
- `src/BuildingBlocks/RetailMedia.Web/Abstractions/IAnalyticsStore.cs`
- `src/Services/RetailMedia.Aggregator/Handlers/CampaignMetricsAggregationHandler.cs`

**What's wrong:** A `"basket"` event currently increments the same `Clicks` counter a click event does, instead of its own `Baskets` counter. This corrupts the `/clicks` endpoint and makes `clickToBasket` mean something other than what it claims.

**Fix summary:**
- Add a `Baskets` field to `CampaignMetricsSnapshot`.
- `"basket"` case increments `Baskets`, not `Clicks`.
- `ClickToBasketRatio` recalculated as `Baskets / Clicks` (true conversion ratio) instead of `Clicks / Impressions` (which was really CTR).

*(Full code for both files was provided earlier in this conversation — reuse that.)*

---

## 2. Four SOLID / Design-Principle Fixes (Priority: Medium — polish for code walkthrough)

All four are small, targeted, and specifically chosen because they're the kind of thing an architect reviewer checks for directly.

### 2a. `IClock` abstraction is never actually used
**Status:** ⬜ Not applied

**Files affected:** `CustomerEvent.cs` (Collector Domain), `CampaignMetricsAggregationHandler.cs` (Aggregator), `CustomerEventEnrichmentHandler.cs` (Processor)

**Issue:** `IClock`/`SystemClock` is registered in DI specifically for testability (inject a fake clock in tests), but every place that needs "now" calls `DateTime.UtcNow` directly instead.

**Fix:** Inject `IClock clock` into each of the three classes above; replace `DateTime.UtcNow` with `clock.UtcNow` (or equivalent method on the interface).

### 2b. Insights query handlers are ~90% duplicated (DRY/OCP)
**Status:** ⬜ Not applied

**File affected:** `src/Services/RetailMedia.Insights/Application/Queries/CampaignMetricHandlers.cs`

**Issue:** `GetCampaignClicksHandler`, `GetCampaignImpressionsHandler`, `GetClickToBasketRatioHandler` are nearly identical — same cache-or-fetch pattern, different field projected. Adding a `Baskets` metric (needed once fix #1 lands) means a fourth near-copy-paste handler.

**Fix:** Extract the shared cache-or-fetch-and-project logic into one private method or a generic base handler, parameterized by which field of `CampaignMetricsSnapshot` to project and which DTO to wrap it in.

### 2c. `ICacheProvider` has two unused methods (ISP)
**Status:** ⬜ Not applied

**File affected:** `src/BuildingBlocks/RetailMedia.Web/Abstractions/ICacheProvider.cs`

**Issue:** `GetAsync` and `SetAsync` are declared on the interface but no call site anywhere uses them — everything goes through `GetOrFetchAsync`.

**Fix:** Either remove the two unused methods from the interface, or add at least one real call site that justifies keeping them. Removing is the simpler, more defensible option unless a specific future use is planned.

### 2d. Event type strings have no single source of truth (magic strings / silent failure)
**Status:** ⬜ Not applied

**Files affected:** `RecordCustomerEventValidator.cs` (defines `AllowedEventTypes` HashSet), `CampaignMetricsAggregationHandler.cs` (re-expresses the same strings as switch-case literals)

**Issue:** The two files independently hardcode the same three strings (`click`, `impression`, `basket`). If they ever drift out of sync, or if a typo passes validation, the Aggregator's `default` case silently does nothing — no log, no error, the event just vanishes from the metrics.

**Fix:**
- Introduce a shared `EventType` enum or a constants class both files reference, so there's one source of truth.
- Change the Aggregator's `default` case to log a warning for unrecognized event types instead of silently no-op'ing.

---

## Not being changed (explicitly out of scope, per earlier agreement)

- `IRetryPolicy` / `IDeadLetterQueue<T>` remain unimplemented interfaces — documented as design intent only, not demo behavior.
- Full session-tracking / Redis-TTL-based windowing implementation — remains a documented design in the architecture doc, not built into the code.
- Idempotency (CorrelationId dedup) — remains a documented trade-off resolution, not implemented in the in-memory demo bus.

---

## Suggested order when picking this back up

1. Basket-counting fix first (it's an active bug, not a style issue).
2. `IClock` injection (cheapest, touches the same files you'll already be in for #1).
3. Event-type constants + Aggregator warning log (natural follow-on from #1, since you'll be in `CampaignMetricsAggregationHandler.cs` anyway).
4. DRY refactor of the three query handlers.
5. `ICacheProvider` cleanup (fully independent, do anytime).
