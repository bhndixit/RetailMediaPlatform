# Gap Analysis & Solutions

This document covers every gap identified when comparing the case study requirements
against the current architecture document and solution. Each gap includes:

- What the gap is and why it exists
- The impact if it is not addressed
- A step-by-step solution for both the architecture document and the codebase
- The reasoning behind each recommended change

---

## Gap 1 — Session Tracking

### What the Gap Is

The case study explicitly requires: *"Explain how you would manage stateful operations,
such as tracking user sessions across multiple events."*

Our current `CustomerEvent` entity has a `CustomerId` but no `SessionId`. Every event
is treated as an independent atomic unit. There is no way to link a sequence of events
from the same user browsing session — product view → ad click → add to basket — into a
connected journey.

### Why It Exists

The initial design focused on individual event capture and aggregation. Session tracking
requires a stateful window: the platform needs to hold state across multiple events that
arrive within a time window (typically 30 minutes of inactivity ends a session). This
was not included in the first pass because the three prescribed API endpoints
(clicks, impressions, clickToBasket) can be answered without session data using simple
counters.

### Impact If Not Addressed

- The `clickToBasket` metric is currently calculated as `clicks / impressions`. This is
  a ratio, not a true conversion. True conversion is: "of all sessions where a customer
  clicked an ad, how many then added a product to basket?" These are different numbers
  and the case study's definition implies the latter.
- Cannot detect funnel drop-off (where in the journey do customers leave?).
- Cannot compute session-level engagement metrics.
- During the interview, this will almost certainly be probed in the 20% "extending the
  design" section.

### Solution

#### Step 1 — Architecture Document Change

In Chapter 7 (End-to-End Data Flow), add a paragraph under "Processing Flow":

> "The Event Processor enriches each event with session context. The retailer SDK
> generates a SessionId when a user lands on the page and passes it with every subsequent
> event during that visit. The Processor links events sharing the same SessionId into a
> session window. A session is considered complete after 30 minutes of inactivity.
> ClickToBasket conversion is calculated at the session level: the percentage of sessions
> containing a click event that also contain a basket event."

In Chapter 10 (Data Architecture), add a `CustomerSession` read model to the analytics
data description:

> "The Aggregator maintains a `CustomerSession` read model alongside `CampaignMetrics`.
> A session record accumulates event types for a given SessionId and is finalised after
> the inactivity window expires. Campaign-level conversion metrics are derived from
> finalised session records."

#### Step 2 — Codebase Changes

**Add `SessionId` to the domain entity** (`CustomerEvent.cs`):

```csharp
public string? SessionId { get; private set; }

public static CustomerEvent Record(
    string tenantId, string campaignId, string customerId,
    string eventType, Guid correlationId, string? sessionId)
{
    var customerEvent = new CustomerEvent
    {
        ...
        SessionId = sessionId
    };
    return customerEvent;
}
```

**Add `SessionId` to the request and command** (`RecordEventRequest`, `RecordCustomerEventCommand`):

```csharp
public sealed record RecordEventRequest(
    string CampaignId,
    string CustomerId,
    string EventType,
    string? SessionId   // optional — retailer SDK provides this
);
```

**Add `SessionId` to `CustomerEventRecorded`** (`RetailMedia.Messaging`):

```csharp
public sealed record CustomerEventRecorded(
    ...
    string? SessionId
) : IIntegrationEvent;
```

**Add `ISessionStore` interface** (`RetailMedia.Web/Abstractions/`):

```csharp
public interface ISessionStore
{
    Task AppendEventAsync(string sessionId, string tenantId,
        string campaignId, string eventType, CancellationToken ct = default);
    Task<SessionSummary?> GetAsync(string sessionId, CancellationToken ct = default);
}

public sealed record SessionSummary(
    string SessionId, string TenantId, string CampaignId,
    bool HasClick, bool HasImpression, bool HasBasket,
    DateTime FirstEventAt, DateTime LastEventAt
);
```

**Why this approach:** The `SessionId` originates from the retailer's browser SDK
(similar to how Google Analytics generates a client ID). The platform never generates
session IDs — it only aggregates events that share one. This keeps the Collector
stateless and the session logic where it belongs: in the Aggregator.

---

## Gap 2 — Real-Time vs Historical Query Distinction

### What the Gap Is

The case study requires: *"Discuss how you would structure these APIs to support both
real-time and historical queries while ensuring performance and reliability."*

The current Insights API returns only the current aggregated state. There are no date
range parameters. A marketer cannot ask "how many clicks did campaign-001 receive
between July 1 and July 7?" The architecture mentions BigQuery for historical analytics
but the API has no way to reach it.

### Why It Exists

The initial implementation wired `IAnalyticsStore` directly to the in-memory metrics
store which holds only the current running total. The distinction between real-time
(current state, Redis) and historical (time-windowed, BigQuery) was described in the
architecture document but not surfaced through the API.

### Impact If Not Addressed

- Marketers cannot generate period-over-period reports.
- The BigQuery investment has no API surface and cannot justify its cost.
- The interview's 20% "extending the design" discussion will probe this directly.

### Solution

#### Step 1 — Architecture Document Change

In Chapter 6 (Solution Overview), update the Insights API component description:

> "The Insights API serves two query modes. Real-time queries (no date range) return
> current aggregated metrics from the Redis cache, with a maximum staleness of 30 seconds.
> Historical queries (with a `from` and `to` date range) are routed to BigQuery, which
> holds the full event history. The routing decision is made inside the query handler —
> the controller is unaware of which store is used."

Update the infrastructure swap table to include:

| Demo | Production |
|---|---|
| `InMemoryHistoricalAnalyticsStore` | BigQuery adapter |

#### Step 2 — Codebase Changes

**Add `DateRange` value object** (`RetailMedia.Core/Domain/`):

```csharp
public sealed record DateRange(DateTime From, DateTime To)
{
    public bool IsHistorical => true; // any range query goes to historical store
}
```

**Add `IHistoricalAnalyticsStore` interface** (`RetailMedia.Web/Abstractions/`):

```csharp
public interface IHistoricalAnalyticsStore
{
    Task<long> GetClicksAsync(string tenantId, string campaignId,
        DateRange range, CancellationToken ct = default);
    Task<long> GetImpressionsAsync(string tenantId, string campaignId,
        DateRange range, CancellationToken ct = default);
    Task<decimal> GetClickToBasketRatioAsync(string tenantId, string campaignId,
        DateRange range, CancellationToken ct = default);
}
```

**Update queries to accept optional date range**:

```csharp
public sealed record GetCampaignClicksQuery(
    string TenantId,
    string CampaignId,
    DateRange? Range = null        // null = real-time, value = historical
) : IRequest<Result<CampaignClicksDto>>;
```

**Update handlers to route based on range**:

```csharp
public async Task<Result<CampaignClicksDto>> Handle(
    GetCampaignClicksQuery query, CancellationToken ct)
{
    if (query.Range is not null)
    {
        var clicks = await _historicalStore.GetClicksAsync(
            query.TenantId, query.CampaignId, query.Range, ct);
        return Result<CampaignClicksDto>.Success(
            new CampaignClicksDto(query.CampaignId, query.TenantId, clicks, DateTime.UtcNow));
    }

    // Real-time path — cache first, analytics store fallback
    var cacheKey = $"campaign:{query.TenantId}:{query.CampaignId}:clicks";
    var metrics = await _cache.GetOrFetchAsync<CampaignMetricsSnapshot>(
        cacheKey, ct => _analyticsStore.GetAsync(query.TenantId, query.CampaignId, ct),
        ttl: TimeSpan.FromSeconds(30), ct);

    return metrics is null
        ? Result<CampaignClicksDto>.Failure(Error.NotFound)
        : Result<CampaignClicksDto>.Success(
            new CampaignClicksDto(metrics.CampaignId, metrics.TenantId,
                metrics.Clicks, metrics.LastUpdated));
}
```

**Update controller** (`CampaignInsightsController`):

```csharp
[HttpGet("{campaignId}/clicks")]
public async Task<IActionResult> GetClicks(
    string campaignId,
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    CancellationToken ct)
{
    var tenant = HttpContext.GetTenantContext();
    var range = (from.HasValue && to.HasValue)
        ? new DateRange(from.Value, to.Value)
        : null;

    var result = await _mediator.Send(
        new GetCampaignClicksQuery(tenant.TenantId, campaignId, range), ct);
    return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
}
```

**Why this approach:** The routing logic lives in the query handler, not in the
controller. The controller only maps HTTP parameters to a query object. This keeps the
controller thin and makes the routing testable without HTTP.

---

## Gap 3 — Data Retention Strategy

### What the Gap Is

The case study requires discussing data retention strategies. The architecture document
describes three data stores but never states how long data lives in each one, when it
is archived, or how deletion requests (GDPR) are handled.

### Why It Exists

Retention was considered an operational concern and deferred. For a demo it is
irrelevant, but the architecture document is the design artefact being evaluated —
it should describe the intended strategy even if it is not implemented in the demo.

### Impact If Not Addressed

- SQL Server will accumulate raw events indefinitely in production, growing without bound.
- No answer to GDPR "right to erasure" requests.
- BigQuery costs are proportional to data volume scanned — no retention policy means
  unbounded cost growth.
- Weak answer in the interview's challenges and trade-offs discussion.

### Solution

#### Architecture Document Change — Add to Chapter 10 (Data Architecture)

Add a "Data Retention" section with the following table:

| Store | Retention Period | Reason | Action on Expiry |
|---|---|---|---|
| SQL Server (raw events) | 90 days | Hot operational queries rarely need data older than 90 days | Archive to cold storage or delete |
| Redis (cache) | 30 seconds per entry | Metrics are continuously updated; stale cache has no value | Automatic TTL expiry |
| BigQuery (analytics) | 3 years | Trend analysis and year-over-year campaign comparisons | Move to long-term BigQuery storage tier (lower cost) |

Also add a GDPR note:

> "Customer interaction events contain a `CustomerId` which may be considered personal
> data depending on jurisdiction. A soft-delete flag on `CustomerEvent` allows the
> system to mark events for erasure without immediate deletion. A scheduled job
> permanently removes flagged records after 30 days, satisfying most right-to-erasure
> timelines."

#### Codebase Change

Add a soft-delete flag to the domain entity:

```csharp
public bool IsDeleted { get; private set; }

public void MarkForErasure() => IsDeleted = true;
```

Add `IDataRetentionPolicy` interface (`RetailMedia.Web/Abstractions/`):

```csharp
public interface IDataRetentionPolicy
{
    Task PurgeExpiredEventsAsync(TimeSpan retentionWindow, CancellationToken ct = default);
    Task EraseCustomerDataAsync(string customerId, string tenantId, CancellationToken ct = default);
}
```

**Why this approach:** Soft delete is safer than hard delete — it gives a grace period
to catch mistakes and satisfies GDPR timelines without immediately destroying data that
may still be needed for aggregations.

---

## Gap 4 — Challenges and Trade-offs Section

### What the Gap Is

The case study explicitly asks: *"Anticipate potential challenges. Discuss how you would
balance trade-offs between real-time performance and data accuracy. Cost trade-offs and
continuous cost monitoring."*

The architecture document has no section addressing challenges or the decisions that
were consciously made at the expense of something else.

### Why It Exists

Architecture documents tend to describe what was chosen, not what was rejected and why.
The interviewer specifically wants to see that the candidate thought critically about
the trade-offs rather than just picking technologies.

### Impact If Not Addressed

The 40% architecture discussion in the interview will surface this immediately. A
candidate who cannot articulate the trade-offs in their own design appears to have
copied an architecture without understanding it.

### Solution

#### Architecture Document Change — Add Chapter 15: Challenges and Trade-offs

**Trade-off 1 — Real-time performance vs data accuracy**

> Pub/Sub delivers events at-least-once, not exactly-once. Under network failures or
> consumer restarts, the same event can be delivered twice. Processing it twice would
> double-count a click.
>
> Chosen resolution: Idempotent event handlers using CorrelationId as a deduplication
> key. This adds a Redis lookup per event but eliminates double-counting.
>
> Alternative rejected: Exactly-once processing via distributed transactions. This
> guarantees accuracy but adds significant latency (2-phase commit across SQL Server
> and Pub/Sub acknowledgement) and reduces throughput under load.

**Trade-off 2 — SQL Server vs NoSQL for raw event storage**

> SQL Server provides ACID transactions and a familiar operational model, but its write
> throughput is lower than purpose-built event stores like Cassandra or DynamoDB at
> extreme scale.
>
> Chosen resolution: SQL Server with horizontal sharding by TenantId for the first
> phase. The `IRawEventStore` interface isolates this decision — migrating to Cassandra
> is an infrastructure change, not a business logic change.
>
> Trigger to revisit: When single SQL Server instance write throughput exceeds 80%
> capacity at peak load.

**Trade-off 3 — Eventual consistency in the analytics pipeline**

> The path from event ingestion to a readable metric involves four steps: Collector
> persists → Pub/Sub delivers → Processor enriches → Aggregator writes. Each step adds
> latency. A marketer querying immediately after an event fires may not see it reflected
> for several seconds.
>
> Chosen resolution: Near-real-time is sufficient for campaign reporting. Metrics
> dashboards do not need sub-second accuracy. This allows each component to be
> independently scaled and deployed without synchronisation.
>
> Not appropriate for: Billing or financial reporting, which would require stronger
> consistency guarantees.

**Trade-off 4 — Shared platform vs dedicated infrastructure per tenant**

> A shared platform (one Pub/Sub topic, one GKE cluster) reduces cost but introduces
> the noisy neighbour problem: one high-volume tenant degrades processing for others.
>
> Chosen resolution: Rate limiting per tenant at the Collector layer controls event
> volume. Pub/Sub partitioning by TenantId ensures fair scheduling. Resource quotas
> at the GKE namespace level isolate compute.

---

## Gap 5 — Event Stream Partitioning Strategy

### What the Gap Is

The case study asks: *"Discuss load balancing and partitioning strategies for event
streams."* The architecture document says Pub/Sub handles event distribution but does
not describe the partitioning key or ordering guarantees.

### Why It Exists

Partitioning is a Pub/Sub configuration decision that sits below the application level.
It was treated as an infrastructure concern and not documented.

### Impact If Not Addressed

- Without ordering keys, events for the same campaign can arrive at the Aggregator
  out of order. A basket event processed before the corresponding click event would
  produce an incorrect clickToBasket ratio momentarily.
- Interview question: "What happens if two events for the same campaign arrive
  simultaneously on different consumers?" — no documented answer.

### Solution

#### Architecture Document Change — Add to Chapter 8 (Deployment Architecture)

> "Google Pub/Sub subscriptions for the Processing and Aggregation consumers are
> configured with message ordering enabled. The ordering key is set to `CampaignId`.
> This guarantees that all events for a given campaign are delivered sequentially to
> the same consumer instance, preserving the order required for accurate session-level
> aggregation.
>
> For the Event Collector publishing path, events are published with the `TenantId`
> as the ordering key to distribute load evenly across partitions while keeping one
> tenant's events on a consistent partition."

#### Codebase Change

Update `IEventBus` to support an optional ordering key:

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, string? orderingKey = null,
        CancellationToken ct = default) where TEvent : class;

    void Subscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>;
}
```

In `RecordCustomerEventHandler`, pass the ordering key when publishing:

```csharp
await eventBus.PublishAsync(new CustomerEventRecorded(...),
    orderingKey: command.TenantId);   // partition by tenant at ingest
```

**Why this approach:** Partitioning by `TenantId` at ingest distributes write load.
Partitioning by `CampaignId` at the consumer ensures ordered aggregation per campaign.
These are two separate Pub/Sub topics with different ordering requirements.

---

## Gap 6 — Specific Key Metrics to Monitor

### What the Gap Is

The case study requires: *"Identify key metrics to monitor platform reliability and
performance."* The architecture document names the monitoring tools (Cloud Monitoring,
OpenTelemetry) but never lists which specific metrics to alert on.

### Why It Exists

Metric selection is typically done during operational readiness review, not during
initial architecture. It was deferred as an implementation detail.

### Impact If Not Addressed

- Operations team has no alerting baseline from day one.
- SLA cannot be defined without knowing what to measure.
- Weak answer in the interview's metrics and monitoring discussion.

### Solution

#### Architecture Document Change — Update Chapter 11 (Observability)

Add a "Key Metrics" table:

**Business Metrics**

| Metric | Alert Threshold | Why |
|---|---|---|
| Event ingestion rate (events/sec) | Drop > 20% from baseline | Indicates retailer SDK failure or upstream outage |
| Processing latency P99 | > 2 seconds | Near-real-time SLA breach |
| Campaign metrics staleness | > 60 seconds since last update | Aggregation pipeline stalled |
| Dead letter queue depth | > 0 | Events are being permanently lost |

**Infrastructure Metrics**

| Metric | Alert Threshold | Why |
|---|---|---|
| Pub/Sub subscription backlog | > 10,000 messages | Consumer is falling behind |
| Redis cache hit ratio | < 70% | Insights API hitting BigQuery on every request |
| SQL Server connection pool | > 80% utilisation | Risk of connection exhaustion |
| GKE pod CPU | > 80% sustained | Scale out trigger |

**Reliability Metrics**

| Metric | Alert Threshold | Why |
|---|---|---|
| 5xx error rate per service | > 1% | Service degradation |
| Health check failures | Any failure | Immediate page |
| Retry rate per handler | > 5% | Indicates downstream instability |

#### Codebase Change

Add structured metric constants to `RetailMedia.Web` so every service uses the same
names and the monitoring dashboards stay consistent:

```csharp
// RetailMedia.Web/Observability/MetricNames.cs
public static class MetricNames
{
    public const string EventIngestionRate    = "retail_media.events.ingested";
    public const string ProcessingLatency     = "retail_media.processing.latency_ms";
    public const string CacheHitRatio         = "retail_media.cache.hit_ratio";
    public const string DeadLetterQueueDepth  = "retail_media.dlq.depth";
}
```

**Why this approach:** Named constants ensure that when the production OpenTelemetry
integration is added, every service uses identical metric names. Dashboards and alerts
defined against these names continue to work without change.

---

## Gap 7 — Cost Monitoring

### What the Gap Is

The case study requires: *"Cost trade-offs and continuous cost monitoring."* There is
no cost monitoring section in the architecture document and no `ICostMonitor` interface
in the codebase, despite it being mentioned in the original Engineering Specification.

### Why It Exists

Cost monitoring was listed in the spec's interface inventory but never implemented. It
is a cross-cutting concern with no natural home in a single service.

### Impact If Not Addressed

- A single tenant generating 10x expected event volume creates unpredictable BigQuery
  and Pub/Sub costs with no early warning.
- No per-tenant cost attribution means billing disputes cannot be resolved.
- Runaway costs are only discovered on the monthly invoice.

### Solution

#### Architecture Document Change — Add to Chapter 11 (Observability)

> "Cost monitoring runs as a background concern across all services. Google Cloud Billing
> Export sends daily cost data to BigQuery. A cost dashboard in Cloud Monitoring shows
> spend by service, by tenant, and by resource type. Alerts fire when daily spend
> exceeds a configurable threshold per tenant."

Add a cost trade-offs section:

> "BigQuery charges per byte scanned. All analytical queries in the Insights API target
> partitioned tables (partitioned by date and TenantId) to minimise scan volume. Redis
> caching reduces BigQuery query frequency. The 30-second cache TTL was chosen as the
> balance between cost (fewer BigQuery hits) and freshness (acceptable staleness for
> reporting)."

#### Codebase Change

Add `ICostMonitor` interface to `RetailMedia.Web/Abstractions/`:

```csharp
public interface ICostMonitor
{
    Task RecordEventProcessedAsync(string tenantId, CancellationToken ct = default);
    Task RecordAnalyticsQueryAsync(string tenantId, string queryType, CancellationToken ct = default);
    Task<TenantCostSummary> GetDailyCostAsync(string tenantId, DateTime date, CancellationToken ct = default);
}

public sealed record TenantCostSummary(
    string TenantId,
    DateTime Date,
    long EventsProcessed,
    long AnalyticsQueriesServed,
    decimal EstimatedCostUsd
);
```

Register a no-op implementation in Infrastructure for the demo:

```csharp
internal sealed class NoOpCostMonitor : ICostMonitor
{
    public Task RecordEventProcessedAsync(string tenantId, CancellationToken ct) =>
        Task.CompletedTask;
    public Task RecordAnalyticsQueryAsync(string tenantId, string queryType, CancellationToken ct) =>
        Task.CompletedTask;
    public Task<TenantCostSummary> GetDailyCostAsync(string tenantId, DateTime date, CancellationToken ct) =>
        Task.FromResult(new TenantCostSummary(tenantId, date, 0, 0, 0m));
}
```

**Why this approach:** The `ICostMonitor` interface sits at the application boundary.
Every event processed and every analytical query served is a billable unit. Recording
these at the application layer means the cost tracking does not depend on cloud billing
lag (which can be 24–48 hours). Production implementation replaces the no-op with a
Cloud Billing Export reader.

---

## Gap 8 — Tenant Resource Allocation and Rate Limiting

### What the Gap Is

The case study requires: *"Discuss strategies for managing tenant-specific
configurations, data access controls, and resource allocation."* There is no rate
limiting in the current design. All tenants share compute resources without any fair
use controls.

### Why It Exists

Rate limiting was deferred as an infrastructure concern. In production, API Gateway can
enforce per-tenant rate limits. But the architecture document does not describe the
strategy, and there is no `IRateLimiter` interface showing where it would plug in.

### Impact If Not Addressed

- Noisy neighbour problem: one tenant sending 100K events/sec degrades processing for
  all other tenants.
- No protection against accidental or malicious event floods.
- Weak answer in the multi-tenancy section of the interview.

### Solution

#### Architecture Document Change — Update Chapter 9 (Security Architecture)

Add to the Security Controls table:

| Area | Implementation |
|---|---|
| Rate Limiting | Per-tenant sliding window counter in Redis. Default: 10,000 events/minute per tenant. Configurable per tenant in the Tenant configuration store. |

#### Codebase Change

Add `IRateLimiter` interface to `RetailMedia.Web/Abstractions/`:

```csharp
public interface IRateLimiter
{
    Task<RateLimitDecision> CheckAsync(string tenantId, CancellationToken ct = default);
}

public sealed record RateLimitDecision(
    bool IsAllowed,
    int RemainingRequests,
    TimeSpan RetryAfter
);
```

Add a no-op implementation in Infrastructure for the demo:

```csharp
internal sealed class NoOpRateLimiter : IRateLimiter
{
    public Task<RateLimitDecision> CheckAsync(string tenantId, CancellationToken ct) =>
        Task.FromResult(new RateLimitDecision(IsAllowed: true, RemainingRequests: int.MaxValue, RetryAfter: TimeSpan.Zero));
}
```

Add rate limit enforcement to `RecordCustomerEventHandler`:

```csharp
public async Task<Result<Guid>> Handle(RecordCustomerEventCommand command, CancellationToken ct)
{
    var decision = await _rateLimiter.CheckAsync(command.TenantId, ct);
    if (!decision.IsAllowed)
        return Result<Guid>.Failure(new Error("RateLimit.Exceeded",
            $"Rate limit exceeded. Retry after {decision.RetryAfter.TotalSeconds}s."));

    // ... rest of handler
}
```

**Why this approach:** Rate limiting in the application handler (not just at the API
Gateway) provides a defence-in-depth. API Gateway handles external traffic, but
internal service-to-service calls also benefit from rate limiting. The `IRateLimiter`
interface means the production Redis sliding window implementation slots in without
touching the handler.

---

## Implementation Priority

Review this list and decide which gaps to address first based on interview date and
available time.

| Gap | Architecture Doc Change | Code Change | Effort | Interview Risk if Skipped |
|---|---|---|---|---|
| 1 — Session Tracking | Medium (2 paragraphs) | Medium (SessionId field, ISessionStore) | 2–3 hours | High — explicitly required |
| 2 — Historical Queries | Medium (routing description) | Medium (IHistoricalAnalyticsStore, query params) | 2–3 hours | High — explicitly required |
| 3 — Data Retention | Small (one table) | Small (soft-delete flag, IDataRetentionPolicy) | 30 minutes | Medium |
| 4 — Challenges & Trade-offs | Large (new chapter) | None | 1 hour | High — explicitly required |
| 5 — Stream Partitioning | Small (one paragraph) | Small (orderingKey on IEventBus) | 30 minutes | Medium |
| 6 — Key Metrics | Medium (two tables) | Small (MetricNames constants) | 1 hour | Medium |
| 7 — Cost Monitoring | Small (one section) | Small (ICostMonitor + NoOp) | 1 hour | Medium |
| 8 — Rate Limiting | Small (one row in table) | Small (IRateLimiter + NoOp) | 1 hour | Medium |

**Recommended order:** 4 → 2 → 1 → 3 → 5 → 6 → 7 → 8

Start with Gap 4 (Challenges and Trade-offs) because it is a documentation-only change
that significantly strengthens the 40% architecture discussion. Then address Gap 2
(Historical Queries) and Gap 1 (Session Tracking) because both are explicitly called
out in the case study and will be probed during the interview.
