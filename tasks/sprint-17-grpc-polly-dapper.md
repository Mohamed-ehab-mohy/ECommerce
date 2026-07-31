# Sprint 17 — Market-Ready Additions (Part 1): gRPC, Polly, Dapper (HIGH priority)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 3.5 | Goal:** Add high-demand job-market technologies to the platform.
> **Source of truth:** `docs/06-system-architecture.md` §4 (layers), `docs/37-coding-standards.md` §6/§8, `docs/30-test-strategy-and-quality-gates.md`.
> **Dependencies:** S1 skeleton, S2–S7 core commerce live. **Blocks:** none.
> **Positioning:** Enablers, not user stories. Run in parallel with or after S16; do NOT delay v1.2 release.
> **Exit:** gRPC + Polly + Dapper all demoed with tests; docs 38/39/40 baseline.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| T-GRPC-001 | gRPC service host + contracts | 5 | [ ] |
| T-GRPC-002 | gRPC services: OrderStatus, CatalogLookup | 5 | [ ] |
| T-RES-001 | Polly policies library (retry/circuit/timeout/bulkhead) | 4 | [ ] |
| T-RES-002 | Wire Polly into HTTP adapters + cache fallback | 4 | [ ] |
| T-DPR-001 | Dapper read models (CQRS read path) | 5 | [ ] |
| T-DPR-002 | Dapper + EF dual-provider query strategy | 4 | [ ] |

---

## T-GRPC-001 — gRPC Service Host + Contracts

### Scope
- Add gRPC server to `ECommerce.API` (separate endpoint, same process) + `ECommerce.Infrastructure`.
- Create `.proto` files under `ECommerce.API/Protos/`:
  - `order.proto` → `OrderStatusService`, `OrderHistoryService`
  - `catalog.proto` → `CatalogLookupService`
- Code-first vs proto-first decision → record ADR; prefer proto-first.
- TLS config for gRPC endpoint; health probe `/grpc.health.v1.Health`.
- Map gRPC services in `Program.cs`; keep REST endpoints intact.

### Acceptance
- [ ] `grpcurl` can list services and call `OrderStatusService.GetByNumber`.
- [ ] gRPC endpoint secured (TLS), REST unaffected.
- [ ] Integration test calls gRPC over Testcontainers-free in-process client.

### Commit
`feat(grpc): add grpc host, protos and order status service`

---

## T-GRPC-002 — gRPC Services: OrderStatus, CatalogLookup

### Scope
- `OrderStatusService.GetByNumber(OrderNumberRequest) → OrderStatusResponse` (orderNumber, status, paymentStatus, timeline).
- `OrderStatusService.Subscribe(StreamRequest) → server-streaming status updates` (pulls from SignalR/event feed).
- `CatalogLookupService.GetBySku(SkuRequest) → ProductSummaryResponse` (id, sku, name, price, available).
- gRPC auth: JWT bearer interceptor (`IMetadataBearerToken`), permission checks on admin methods.

### Acceptance
- [ ] Unary + server-streaming calls work with JWT.
- [ ] 403/401 mapped to gRPC status codes (PermissionDenied/Unauthenticated).
- [ ] Unit tests for interceptors; integration tests for services.

### Commit
`feat(grpc): order status and catalog lookup services with jwt auth`

---

## T-RES-001 — Polly Policies Library

### Scope
- `ECommerce.Infrastructure/Resilience/` with Polly (v8 `ResiliencePipeline`):
  - Retry (jittered, per-adapter config)
  - Circuit breaker (consecutive failures, half-open)
  - Timeout (per-call, overall)
  - Bulkhead (limited concurrency for PSP/carrier calls)
- Named policies registered in DI: `psp`, `carrier`, `tax`, `fx`, `email`, `search`.
- Config-driven via `IOptions` (retry counts, break thresholds, timeouts).
- Metrics/telemetry per policy (otel + log on open/close).

### Acceptance
- [ ] Unit tests prove: retry fires N times, circuit opens then half-opens, timeout aborts.
- [ ] Policy failures exposed as `problems/upstream-unavailable` (502).
- [ ] Dashboards show circuit state metric.

### Commit
`feat(resilience): polly policies library with telemetry`

---

## T-RES-002 — Wire Polly into HTTP Adapters + Cache Fallback

### Scope
- Apply `ResiliencePipeline` to every `IHttpClientFactory` typed client (PSP, carrier, tax, FX, email).
- Add cache-fallback on provider failure where cacheable (shipping rates, FX, product enrich): serve stale + `Warning` log + metric `provider_fallback_count`.
- Ensure idempotent operations safe under retry (no double PSP charge — retry only on idempotent ops).

### Acceptance
- [ ] Kill provider in staging → circuit opens, fallback data served, no double-charge.
- [ ] Contract tests pass through policies.

### Commit
`feat(resilience): wire polly into adapters with cache fallback`

---

## T-DPR-001 — Dapper Read Models (CQRS Read Path)

### Scope
- Add `Dapper` + `Npgsql` to `ECommerce.Infrastructure`.
- Introduce `IReadModelStore` used by query services; implement `DapperReadModelStore` (raw SQL to DTOs) alongside EF read services.
- Read models first: product summary, order history (cursor), stock availability, report aggregations.
- Parameterized SQL only; SQL lives in dedicated `Sql/` folder or inline constants — no string concat.

### Acceptance
- [ ] Same query via EF and Dapper returns identical DTOs (contract tests).
- [ ] Benchmarked (BenchmarkDotNet or k6) — Dapper path faster or equal; results recorded.
- [ ] Architecture tests: read model store used only by query paths, never write paths.

### Commit
`feat(cqrs): dapper read models for read path`

---

## T-DPR-002 — Dapper + EF Dual-Provider Query Strategy

### Scope
- Document + enforce: **writes = EF Core only, reads = EF or Dapper** decision rule (ADR).
- DI: register `IReadModelStore` (Dapper) + `IDbConnectionFactory`; repositories keep EF.
- Config toggle `QueryProvider: Ef|Dapper` per context for A/B benchmarking.
- Keep transaction discipline: reads on replica connection, writes on primary.

### Acceptance
- [ ] Toggle switches read path at runtime without code change.
- [ ] Benchmark comparison recorded in `docs/40-dapper-read-models.md` + `34` report.
- [ ] No Dapper reference outside Infrastructure (architecture test).

### Commit
`feat(cqrs): dual-provider query strategy with toggle`

---

## Sprint Exit
- [ ] gRPC services demoed (grpcurl + integration tests); docs 38 baseline.
- [ ] Polly policies verified (circuit open/fallback/no double-charge); docs 39 baseline.
- [ ] Dapper read models benchmarked; docs 40 baseline.
- [ ] CI green; all ADRs recorded.
