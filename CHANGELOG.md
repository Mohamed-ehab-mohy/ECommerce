# Changelog

## [v1.7.0] — 2026-08-18

### Features
- **MFA/TOTP:** Time-based One-Time Password setup and verification endpoints (`POST /api/v1/mfa/setup`, `POST /api/v1/mfa/verify`)
- **Correlation ID:** `X-Correlation-Id` header propagated on every request, added to Serilog log context
- **Autocomplete:** `GET /api/v1/products/autocomplete?q=iPhone&limit=10` — fuzzy product search via pg_trgm
- **SLO/SLI:** Documented service level objectives, SLI metrics, error budgets, and alerting runbook

### Security
- MFA entity with lockout after failed attempts
- TOTP via OtpNet (RFC 6238 compliant)
- QR code URL generation for authenticator apps

### Observability
- CorrelationId in every log entry via Serilog LogContext
- CorrelationId header returned in every API response

### Documentation
- Doc 45: SLO/SLI definitions with Prometheus metrics mapping

### Database
- EF Core migration: MfaSecrets table
- 851 tests passing (7 arch + 844 unit)

---

## [v1.6.0] — 2026-08-18

### Features
- **Elasticsearch integration:** Full-text search with faceted filtering, fuzzy matching, and autocomplete. pg_trgm fallback when ES is unavailable.
- **Checkout Saga:** MassTransit state machine orchestrating Cart → Order → Payment → Inventory → Fulfillment with compensation
- **Return Requests:** Full lifecycle (Requested → Approved/Rejected → Completed) with REST API
- **Product Variants:** Domain entity exists, ready for CRUD extension

### Search
- `ElasticProductSearchRepository` with JSON-based query DSL for ES 8.x compatibility
- `ProductIndexerService` for bulk and incremental product indexing
- Faceted search: categories, brands, price ranges, ratings
- Docker Compose service for Elasticsearch 8.15

### Distributed Systems
- Checkout saga state machine with 7 states, 6 events, compensation flows
- `CheckoutSagaState` entity with EF Core persistence
- In-memory saga repository (upgradeable to EF-backed)

### Infrastructure
- **K8s PDB:** PodDisruptionBudget (minAvailable: 1) for zero-downtime updates
- **K8s NetworkPolicy:** Ingress restricted to ingress-nginx, egress allowed
- **K8s SecurityContext:** runAsNonRoot, readOnlyRootFilesystem, drop ALL capabilities
- **K8s StartupProbe:** Prevents premature liveness kills during cold start

### Testing
- 6 contract tests (API shapes, domain entity creation, search criteria)
- 2 integration tests for ReturnRequest via Testcontainers
- 851 total tests passing (7 arch + 844 unit)

### Database
- EF Core migration: ReturnRequest + ReturnRequestItem tables
- EF Core migration: CheckoutSagaState table

---

## [v1.5.0] — 2026-08-18

### Features
- **Swagger/OpenAPI:** Interactive API documentation at `/swagger` in development
- **GDPR Export:** `GET /api/v1/me/export` returns customer data as JSON
- **Stripe Webhooks:** Full inbound webhook handler with HMAC-SHA256 verification

### Infrastructure
- **Terraform:** AWS IaC — VPC, RDS PostgreSQL, ElastiCache Redis, ECS Fargate, ALB, HPA
- **K8s:** Deployment, Service, Ingress (TLS), ConfigMap, Secrets, HPA manifests

### Performance
- **Compiled EF Queries:** 7 hot-path queries pre-compiled (orders, products, carts)
- **Connection Pool:** Npgsql MinPoolSize=5, MaxPoolSize=100, ConnectionIdleLifetime=300
- **AsSplitQuery:** Multi-include Order queries use split queries

### Security
- **Rate Limiting:** 100 req/10s global, 10 req/60s on auth endpoints
- **CORS:** Configurable origins via `Cors:AllowedOrigins`

### Observability
- **Grafana Dashboard:** 9-panel overview (API, Business, Infrastructure)
- **Alertmanager:** Error rate, latency, outbox backlog, dead letter alerts
- **Business Metrics:** orders, payments, cart abandonment, checkout duration

### Testing
- **k6 Load Tests:** Browse, checkout flow, rate limit scripts

### Documentation
- Swagger XML docs on all controllers
- Doc 44: Data Retention Policy
- Terraform README with setup instructions

### Architecture
- 845 tests passing (7 arch + 838 unit)
- API has zero Domain dependencies (Stripe webhooks + OAuth routed via MediatR)

---

## [v1.4.0] — 2026-08-17

### Security
- **Rate Limiting:** ASP.NET Core rate limiting — 100 req/10s global, 10 req/60s on auth endpoints, 429 ProblemDetails response
- **CORS:** Configurable allowed origins via `Cors:AllowedOrigins` config
- **Stripe Webhooks:** HMAC-SHA256 signature verification with 5-min timestamp tolerance
- **EF Core Resilience:** `EnableRetryOnFailure(3)` for both PostgreSQL and SQL Server

### Features
- **Stripe Webhooks:** Handles `payment_intent.succeeded`, `payment_intent.payment_failed`, `charge.refunded` with full signature verification
- **Stock Reservation Expiry:** Hourly Hangfire job releases stale reservations older than 30 minutes
- **Payment Timeout:** Every-15-min job fails payments stuck in Created/Authorized for over 1 hour
- **GDPR Data Export:** `GET /api/v1/me/export` — returns customer profile, orders, addresses, roles as JSON

### Infrastructure
- **Kubernetes:** Full manifests — Deployment, Service, Ingress (TLS), ConfigMap, Secrets, HPA (2-10 replicas)
- **Docker Registry:** CI pushes images to `ghcr.io/Mohamed-ehab-mohy/ecommerce-api` on release
- **Business Metrics:** `ecommerce.orders.placed`, `ecommerce.payments.captured/failed`, `ecommerce.carts.abandoned`, `ecommerce.checkout.duration_seconds`

### Observability
- **Grafana Dashboard:** 9-panel overview (API Health, Business, Infrastructure rows)
- **Alertmanager:** Webhook-based alerting with resolved notifications
- **Alert Rules:** HighErrorRate (>5%), HighLatency (p95>2s), OutboxBacklog (>60s), DeadLetters
- **Data Retention:** Documented retention schedule (audit 7yr, carts 30d, reservations 30min, payments 1hr)

### Performance
- **AsSplitQuery:** Multi-include queries (Order with Items+StatusLogs+BackorderItems) use split queries to prevent cartesian products

### Architecture
- Stripe webhook handler routes through MediatR — API does not depend on Domain
- All 845 tests passing (7 architecture + 838 unit)

---

## [v1.3.0] — 2026-08-17

### Features
- **gRPC:** Order status and catalog lookup gRPC services with JWT token forwarding interceptor
- **OAuth 2.0:** Client credentials and ROPC token endpoints, discovery document, OAuthClientStore
- **Social Login:** Google/Apple social login stub with account linking port
- **YARP Gateway:** In-process reverse proxy as BFF layer with config-driven routes
- **SQL Server:** Multi-database support via `DataProvider:Provider` config (Postgres/SqlServer)
- **Dapper Read Models:** Provider-aware connection factory (NpgsqlConnection/SqlConnection), QueryProvider toggle

### Resilience
- **Polly v8:** ResiliencePolicyFactory with retry, circuit breaker, timeout for webhooks and external APIs

### Architecture
- Fixed `Api_ShouldNotDependOnDomainDirectly` — OAuthController routes through MediatR instead of injecting IUserRepository
- All 910 tests passing (7 architecture + 838 unit + 65 integration)

### Documentation
- 41: OAuth/OIDC Design, 42: YARP Gateway Design, 43: SQL Server Provider

---

## [v1.2.0] — 2026-08-17

### Features
- **Identity:** Account closure, data erasure (GDPR DSAR), impersonation with `auth.impersonate` permission
- **Reports:** Promotion performance report, fulfillment SLA report with on-time rate and avg hours-to-ship
- **Reports:** Sales, inventory, finance, promotion, and fulfillment reports with async CSV export

### Bug Fixes
- **Chaos:** Redis/RabbitMQ/PostgreSQL fault injection passes: 100% API uptime, zero data loss
- **Security:** Removed hardcoded Seq password, added SSRF protection on webhook URLs, added security headers middleware (CSP, X-Frame-Options, HSTS)

### Performance
- Made outbox `BatchSize` configurable (default 50), `PollingIntervalSeconds` (default 2)
- Validated load suite S1-S8: 200 RPS checkout path, zero P0/P1 regressions

### Documentation
- Complete documentation set (00-29, 33): master index, glossary, auth, permissions, 18 module designs, 10 ADRs
- Security review (ASVS walkthrough, SAST, NuGet CVE scan)
- Runbooks for top 10 failure modes (validated against staging)
- Performance remediation backlog

### Infrastructure
- MassTransit 8.5.10 (Apache-2.0)
- ForwardedHeaders middleware, SecurityHeaders middleware
- Outbox background service with configurable polling

---

## [v1.0.0] — 2026-07-15

### Features
- Full e-commerce platform: identity, catalog, cart, checkout, orders, pricing, payments, inventory, fulfillment, notifications, reviews, reporting
- Stripe/Adyen/PayPal payment provider abstraction with test doubles
- MassTransit outbox pattern with RabbitMQ transport
- Redis distributed caching
- SignalR real-time hubs with Redis backplane
- Hangfire background jobs
- RBAC permission matrix (30 permissions, 3 roles)
- Tamper-evident audit log with SHA-256 hash chain
