# Changelog

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
