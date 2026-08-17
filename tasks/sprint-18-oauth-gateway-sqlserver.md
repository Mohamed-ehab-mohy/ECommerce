# Sprint 18 — Market-Ready Additions (Part 2): OAuth/OIDC, YARP Gateway, SQL Server (MEDIUM priority)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 3.5 | Goal:** Add medium-demand job-market technologies to the platform.
> **Source of truth:** `docs/09-security-architecture.md` §6/§7, `docs/08-api-design.md` §3, `docs/37-coding-standards.md` §9.
> **Dependencies:** S2 (identity), S8–S13 (commerce). **Blocks:** none.
> **Positioning:** Optional but CV-strong. Run after S16/S17; do NOT delay v1.2.
> **Exit:** OAuth/OIDC + YARP + SQL Server demoed with tests; docs 41/42/43 baseline.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| T-OAU-001 | OpenIddict (OIDC server) setup | 5 | [x] |
| T-OAU-002 | Social login (Google + Apple) | 4 | [x] |
| T-OAU-003 | External API tokens (scoped client credentials) | 4 | [x] |
| T-YRP-001 | YARP reverse proxy (BFF gateway) | 5 | [x] |
| T-SQL-001 | SQL Server provider for EF | 4 | [x] |
| T-SQL-002 | Dapper SQL Server read models + provider switch | 4 | [x] |

---

## T-OAU-001 — OpenIddict (OIDC Server) Setup

### Scope
- Add `OpenIddict` (or IdentityServer4-compatible) as OIDC provider on top of ASP.NET Identity.
- Configure authorization code + PKCE + refresh tokens (keep existing JWT for API).
- Endpoints: `/connect/token`, `/connect/authorize`, `/connect/revoke`, `.well-known/openid-configuration`.
- Clients: web (code+PKCE), mobile (native), trusted API (client credentials).
- Consent screen + scopes (`openid profile email offline_access` + custom `orders.read` etc.).

### Acceptance
- [ ] Authorization Code + PKCE flow works end-to-end in integration test.
- [ ] Discovery document valid; token introspection works.
- [ ] Refresh rotation from S2 reused.

### Commit
`feat(oidc): openiddict server with authorization code and pkce`

---

## T-OAU-002 — Social Login (Google + Apple)

### Scope
- External identity providers via ASP.NET Identity + OpenIddict: Google + Apple sign-in.
- Account linking: same email → link with warning + audit; conflict → prompt.
- PII-safe: store only provider subject + email; never tokens.

### Acceptance
- [ ] Google + Apple test accounts sign in; linking works; audit records provider.
- [ ] Conflict path handled without data loss.

### Commit
`feat(oidc): google and apple social login`

---

## T-OAU-003 — External API Tokens (Scoped Client Credentials)

### Scope
- Client-credentials grants for B2B/partner API access (registered apps, scoped permissions).
- Token introspection + audience validation for partner endpoints; revoke/rotate app secrets.

### Acceptance
- [ ] Partner app obtains scoped token; forbidden scope → 403.
- [ ] Secret rotation revokes old credentials.

### Commit
`feat(oidc): client credentials for partner api access`

---

## T-YRP-001 — YARP Reverse Proxy (BFF Gateway)

### Scope
- Add `Yarp.ReverseProxy` to a gateway project (or API host) as BFF layer.
- Route: `/api/v1/*` → API, `/grpc/*` → gRPC, `/swagger/*`, `/hangfire/*`, `/hubs/*` → API.
- Rate limiting at gateway (Redis), request ID correlation, security headers, CORS at gateway.
- Forwarded headers (X-Forwarded-For) + TLS termination notes.
- Health check on gateway.

### Acceptance
- [ ] All routes proxied correctly (REST + gRPC + SignalR + dashboard).
- [ ] Gateway-level rate limit enforced; headers set; traceId propagated.
- [ ] Config via `appsettings` (YARP transform-friendly), not code.

### Commit
`feat(gateway): yarp reverse proxy as bff gateway`

---

## T-SQL-001 — SQL Server Provider for EF

### Scope
- Add `Microsoft.EntityFrameworkCore.SqlServer` to `ECommerce.Infrastructure`.
- Provider selection via config `DataProvider: Postgres|SqlServer`; same model, migrations per provider.
- Migration strategy: keep provider-specific migration folders (`Migrations/Postgres`, `Migrations/SqlServer`).
- Document feature parity gaps (partitioning, RLS differences) in `docs/43-sql-server-provider.md`.

### Acceptance
- [ ] Full schema migrates + seeds on SQL Server (local/container).
- [ ] Integration tests run against SQL Server container variant.
- [ ] CI matrix runs both providers.

### Commit
`feat(data): sql server ef provider with migrations`

---

## T-SQL-002 — Dapper SQL Server Read Models + Provider Switch

### Scope
- `DapperReadModelStore` gains SQL Server dialect (`SqlConnection`).
- Provider-aware SQL per query (Postgres vs SQL Server parameterization/pagination differences).
- Config toggle: `QueryProvider: Ef|Dapper` + `DataProvider: Postgres|SqlServer` (4 combinations tested in CI).

### Acceptance
- [ ] Read models identical across provider combinations.
- [ ] CI matrix: 2 data providers × 2 query providers.
- [ ] Performance comparison recorded.

### Commit
`feat(data): dapper sql server read models with provider switch`

---

## Sprint Exit
- [x] OIDC flows (code+PKCE, social, client-credentials) tested; docs 41 baseline.
- [x] YARP gateway routing verified (REST/gRPC/SignalR); docs 42 baseline.
- [x] SQL Server + Dapper provider matrix green in CI; docs 43 baseline.
- [x] CI green; ADRs recorded.
