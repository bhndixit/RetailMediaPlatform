# Retail Media Intelligence Platform

Architecture reference implementation demonstrating a cloud-native, multi-tenant event processing platform built with .NET 9 and Clean Architecture.

---

## What This Is

A demonstration codebase that shows how the platform is organised, how services relate to each other, and how the architecture scales. Every production infrastructure dependency (Google Pub/Sub, Redis, BigQuery) is replaced with a lightweight in-memory stand-in behind an interface. Swapping to production infrastructure requires only changes to `RetailMedia.Infrastructure` — no business logic changes.

---

## Solution Structure

```
RetailMediaPlatform.sln

Doc/
  CaseStudy_Document.docx           ← Architecture design document
  Case Study.docx                   ← Original case study assignment brief

src/
  BuildingBlocks/
    RetailMedia.Core                ← Domain primitives, Result<T>, base entities
    RetailMedia.Messaging           ← Integration event contracts, IEventBus
    RetailMedia.Web                 ← HTTP middleware, TenantContext, shared interfaces

  Services/
    RetailMedia.Collector           ← Receives and persists customer interaction events (API)
    RetailMedia.Processor           ← Normalises and enriches events (Worker)
    RetailMedia.Aggregator          ← Builds campaign metrics read model (Worker)
    RetailMedia.Insights            ← Serves analytical queries to dashboards (API)

  Infrastructure/
    RetailMedia.Infrastructure      ← In-memory implementations of all interfaces

  Demo/
    RetailMedia.Demo                ← Composite host: runs all services in one process
```

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 (v17.8+) or JetBrains Rider

---

## Build

### Visual Studio

1. Open `RetailMediaPlatform.sln`
2. Wait for NuGet packages to restore (status bar shows **Ready**)
3. Press **Ctrl+Shift+B** to build the full solution

### Command Line

```bash
dotnet build RetailMediaPlatform.sln --configuration Release
```

Expected output: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Run the Demo

### Visual Studio

1. In Solution Explorer, right-click **`RetailMedia.Demo`** → **Set as Startup Project**
2. Press **F5** (with debugger) or **Ctrl+F5** (without)
3. Your browser opens automatically at `http://localhost:5100`

### Command Line

```bash
dotnet run --project src/Demo/RetailMedia.Demo/RetailMedia.Demo.csproj
```

Then open `http://localhost:5100` in your browser.

---

## End-to-End Testing

The Swagger UI at `http://localhost:5100` is the primary testing interface. No token or login is required in the demo environment.

### Step 1 — Record a customer event

1. Expand **`POST /api/v1/events`**
2. Click **Try it out**
3. The `X-Tenant-Id` dropdown appears automatically — select **`tenant-alpha`**
4. Enter the request body:

```json
{
  "campaignId": "campaign-001",
  "customerId": "customer-xyz",
  "eventType": "click"
}
```

5. Click **Execute**
6. Expected response — `202 Accepted`:

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

The console window shows the full processing chain:

```
[INF] Publishing CustomerEventRecorded
[INF] Processing event for campaign campaign-001
[INF] Aggregating metrics | EventType: click | Tenant: tenant-alpha
[INF] Metrics updated: Clicks=1, Impressions=0, Baskets=0
```

---

### Step 2 — Query the metric

1. Expand **`GET /api/v1/ad/{campaignId}/clicks`**
2. Click **Try it out**
3. Select `X-Tenant-Id: tenant-alpha`
4. Set `campaignId` to `campaign-001`
5. Click **Execute**
6. Expected response — `200 OK`:

```json
{
  "campaignId": "campaign-001",
  "tenantId": "tenant-alpha",
  "clicks": 1,
  "asOf": "2026-07-08T10:00:00Z"
}
```

Send the POST a few more times and re-query to see the counter increment.

---

### Step 3 — Test all three metric types

Send separate POST requests with different `eventType` values, then query each endpoint:

| eventType | Query endpoint | Shows |
|---|---|---|
| `click` | `GET /api/v1/ad/{id}/clicks` | Number of ad clicks |
| `impression` | `GET /api/v1/ad/{id}/impressions` | Number of ad views |
| `basket` | `GET /api/v1/ad/{id}/clickToBasket` | Basket conversion ratio (baskets ÷ clicks) |

**Example — after 4 clicks and 2 basket events:**

```json
{
  "campaignId": "campaign-001",
  "tenantId": "tenant-alpha",
  "ratio": 0.5,
  "asOf": "2026-07-08T10:00:00Z"
}
```

A ratio of `0.5` means 50% of users who clicked also added a product to their basket.

---

### Step 4 — Tenant isolation

This demonstrates the core multi-tenancy guarantee.

1. Send 3 click events with `X-Tenant-Id: tenant-alpha`, `campaignId: campaign-001`
2. Send 1 click event with `X-Tenant-Id: tenant-beta`, `campaignId: campaign-001`
3. Query `GET /api/v1/ad/campaign-001/clicks` with `tenant-alpha` → `clicks: 3`
4. Query the same endpoint with `tenant-beta` → `clicks: 1`

Same campaign ID, completely isolated data per tenant.

---

### Step 5 — Validation

Send a POST with an invalid `eventType`:

```json
{
  "campaignId": "campaign-001",
  "customerId": "customer-xyz",
  "eventType": "purchase"
}
```

Expected response — `400 Bad Request`:

```json
{
  "code": "General.Validation",
  "description": "EventType must be one of: click, impression, basket."
}
```

---

### Step 6 — Health checks

Open these URLs directly in the browser while the Demo is running:

| Endpoint | Expected |
|---|---|
| `http://localhost:5100/health/live` | `Healthy` |
| `http://localhost:5100/health/ready` | `Healthy` |

---

## Reference

### Valid configuration values

| Setting | Values |
|---|---|
| Tenant IDs | `tenant-alpha`, `tenant-beta` |
| Event types | `click`, `impression`, `basket` |
| Demo URL | `http://localhost:5100` |

### Insights API endpoints

| Endpoint | Description |
|---|---|
| `POST /api/v1/events` | Record a customer interaction event |
| `GET /api/v1/ad/{campaignId}/clicks` | Number of ad clicks for a campaign |
| `GET /api/v1/ad/{campaignId}/impressions` | Number of ad impressions for a campaign |
| `GET /api/v1/ad/{campaignId}/clickToBasket` | Basket conversion ratio for a campaign |

### Infrastructure swap table

| Demo (current) | Production replacement | Change required |
|---|---|---|
| `InMemoryEventBus` | Google Pub/Sub | `RetailMedia.Infrastructure` only |
| `InMemoryCache` | Redis | `RetailMedia.Infrastructure` only |
| `InMemoryAnalyticsStore` | BigQuery | `RetailMedia.Infrastructure` only |
| `ConfigurationTenantProvider` | Tenant Management Service | `RetailMedia.Infrastructure` only |
| `DemoAuthHandler` | OAuth2 / OpenID Connect | `RetailMedia.Infrastructure` only |

No business logic changes are required when moving to production infrastructure.

---

## Architecture Principles Demonstrated

| Principle | Where to see it |
|---|---|
| Clean Architecture | `Domain/` has zero external NuGet references; infrastructure implements interfaces it never defines |
| CQRS | `Commands/` and `Queries/` are separate folders in every service; handlers never mix read and write |
| Multi-tenancy | `TenantMiddleware` resolves tenant once per request; every handler receives it via `TenantContext` |
| Event-driven | `Collector` publishes, `Processor` and `Aggregator` subscribe; no direct service-to-service references |
| Observability | Every log line carries `TenantId` and `CorrelationId`; `/health/live` and `/health/ready` on every service |
| Resilience interfaces | `IDeadLetterQueue`, `IRetryPolicy`, `ICacheProvider.GetOrFetchAsync` signal production resilience patterns |
