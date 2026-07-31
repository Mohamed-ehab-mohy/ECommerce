# Sprint 1 — Foundations & Continuous Integration (T-FND-001..T-OPS-001)

> **From:** Tech Lead (Senior) — **To:** Backend Engineer
> **Phase 0 | Theme:** Technical runway. **Goal:** One-command dev stack and a green CI pipeline.
> **Source of truth:** `docs/06-system-architecture.md` §4, `docs/37-coding-standards.md`, `docs/30-test-strategy-and-quality-gates.md`, `docs/03c-sprint-plan.md`.
> **Dependencies:** none. **Blocks:** all later sprints.
> **Exit (M1):** Architecture tests pass; skeleton slices compile; ADR-001/002 recorded.

---

## Sprint Scope

| ID | Task | Points | Status |
|----|------|:------:|--------|
| T-FND-001 | Solution skeleton + Clean Architecture layering + DI | 5 | [x] |
| T-FND-002 | docker-compose stack (PostgreSQL, Redis, RabbitMQ, Seq, Prometheus, Grafana) | 5 | [x] |
| T-FND-003 | CI pipeline (build + static analysis + secret scan) | 3 | [x] |
| T-OBS-001 | Serilog + OpenTelemetry + health checks baseline | 3 | [x] |
| T-FND-004 | EF Core + first migration + Testcontainers harness | 4 | [x] |
| T-FND-005 | Domain skeleton (BaseEntity, Result, errors) | 2 | [x] |
| T-OPS-001 | README + onboarding runbook | 1 | [x] |

---

## T-FND-001 — Solution Skeleton + Clean Architecture + DI

See original senior assignment: reproduced below.

### Scope
- Create solution + 6 projects: `src/ECommerce.API` (web), `src/ECommerce.UseCases` (classlib), `src/ECommerce.Domain` (classlib), `src/ECommerce.Infrastructure` (classlib), `tests/ECommerce.UnitTests` (xunit), `tests/ECommerce.ArchitectureTests` (xunit).
- TFM `net10.0`; references: API→UseCases+Infrastructure, Infrastructure→UseCases+Domain, UseCases→Domain only, Domain→nothing.
- Delete template files (`Class1.cs`, `WeatherForecast`).
- Add `Directory.Build.props` (nullable, warnings-as-errors, latest analyzers) + `.editorconfig`.
- Add DI extension methods per layer: `AddApplication`, `AddInfrastructure`, `AddApi` (empty contracts).
- Minimal `Program.cs` calling the three `Add*` methods.

### Acceptance
- [ ] `dotnet build -warnaserror` → zero warnings.
- [ ] `dotnet test` green.
- [ ] `dotnet format --verify-no-changes` clean.
- [ ] Solution opens in IDE without errors.
- [ ] No code comments.

### Commit
`chore(skeleton): scaffold clean architecture solution`

---

## T-FND-002 — docker-compose Stack

### Scope
Create `docker-compose.yml` + `.env.example` at repo root with services:

| Service | Image | Port(s) | Volume |
|---------|-------|---------|--------|
| postgres | postgres:16-alpine | 5432 | named volume |
| redis | redis:7-alpine | 6379 | named volume |
| rabbitmq | rabbitmq:3.13-management | 5672, 15672 | named volume |
| seq | datalust/seq:latest | 5341 | named volume |
| prometheus | prom/prometheus:latest | 9090 | bind `monitoring/prometheus` |
| grafana | grafana/grafana:latest | 3000 | bind `monitoring/grafana` |

- Healthchecks for postgres (`pg_isready`), redis (`redis-cli ping`), rabbitmq.
- Named network `ecommerce`.
- Non-root best effort; documented credentials in `.env.example` only.

### Acceptance
- [ ] `docker compose up -d` → all services healthy.
- [ ] `docker compose down` clean; volumes persist on restart.
- [ ] Ports don't collide with host defaults documented.

### Commit
`chore(docker): add dev stack with postgres, redis, rabbitmq, seq, prometheus, grafana`

---

## T-FND-003 — CI Pipeline (Build + Static Analysis + Secret Scan)

### Scope
Create `.github/workflows/ci.yml` (GitHub Actions) on `main` + PRs:
- Job 1: `dotnet restore/build -warnaserror` (net10.0).
- Job 2: `dotnet test` (UnitTests + ArchitectureTests).
- Job 3: `dotnet format --verify-no-changes`.
- Job 4: secret scan (gitleaks-action or `trufflehog`).
- Job 5: dependency scan (Dependabot enabled via `.github/dependabot.yml`).
- Cache `~/.nuget/packages`.
- Timeouts per job; fail-fast.

### Acceptance
- [ ] Green run on a fresh commit; logs show each gate.
- [ ] A deliberately introduced warning fails the build (verified once manually).
- [ ] Secrets in test files would fail the scan (verified once manually).

### Commit
`ci: add build, test, format, secret-scan and dependency gates`

---

## T-OBS-001 — Serilog + OpenTelemetry + Health Checks Baseline

### Scope
In `ECommerce.API` + `ECommerce.Infrastructure`:
- Serilog: console + Seq (structured logs; `SeqUrl` from config).
- OpenTelemetry: OTLP exporter (traces + metrics) with service name `ecommerce-api`.
- Health checks: `/health/live` (liveness, always) + `/health/ready` (readiness: postgres ping).
- Custom JSON health response writer (no default text).
- Prometheus metrics endpoint on `/metrics` (via OTel or prometheus-net).

### Acceptance
- [ ] App starts; logs appear in Seq at `http://localhost:5341`.
- [ ] `/health/live` and `/health/ready` return 200 with JSON; ready goes 503 when postgres down.
- [ ] `/metrics` returns Prometheus text (contains `process_*`).
- [ ] Trace emitted for a sample request visible in Seq.

### Commit
`feat(observability): add serilog, opentelemetry, health checks and metrics baseline`

---

## T-FND-004 — EF Core + First Migration + Testcontainers Harness

### Scope
In `ECommerce.Infrastructure`:
- Add `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Add `DbContext` factory + `IDesignTimeDbContextFactory`.
- Empty `Data/Configuration` folder + `ApplyConfigurationsFromAssembly`.
- First migration `InitialMigration` (schema empty or minimal seed-friendly).
- Create `tests/ECommerce.IntegrationTests` project (xunit) with Testcontainers:
  - `PostgreSqlContainer` fixture; applies migrations; asserts connectivity.
  - `RedisContainer` + `RabbitMqContainer` fixtures (connectivity smoke).

### Acceptance
- [ ] `dotnet ef migrations list` shows `InitialMigration`.
- [ ] Integration test boots real Postgres via Testcontainers and passes.
- [ ] Tests skip gracefully when Docker unavailable (marked skip, not fail).

### Commit
`feat(infra): add EF core with npgsql, initial migration and testcontainers harness`

---

## T-FND-005 — Domain Skeleton (BaseEntity, Result, Errors)

### Scope
In `ECommerce.Domain` (per `06a-domain-model.md`, `37-coding-standards.md` §6):
- `BaseEntity<TId>`: `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted` (soft-delete support).
- `Result` / `Result<T>`: success/failure monadic type with implicit conversions.
- `Error` record + `ErrorType` enum.
- `DomainException` base.
- No EF annotations in Domain. No comments.

### Acceptance
- [ ] Unit tests in `ECommerce.UnitTests` covering Result success/failure conversions.
- [ ] Domain project has zero package references.

### Commit
`feat(domain): add base entity, result and error primitives`

---

## T-OPS-001 — README + Onboarding Runbook

### Scope
- Root `README.md`: project overview, prerequisites (.NET 10, Docker), quick start (`docker compose up`, `dotnet run --project src/ECommerce.API`), health URLs, links to `docs/`.
- `docs/36-developer-onboarding-guide.md` placeholder→ complete minimal version: setup, common commands, test commands, troubleshooting.

### Acceptance
- [ ] A new developer follows README from clean clone to running API + healthy stack in < 30 min.
- [ ] All commands verified on a clean machine state.

### Commit
`docs: add readme and developer onboarding guide`

---

## Sprint Exit (M1)

- [ ] Architecture tests pass; skeleton slices compile.
- [ ] ADR-001 (modular monolith) + ADR-002 (skeleton/layering) recorded in `docs/33-architecture-decision-records.md`.
- [ ] `docker compose up` runs full stack; health endpoints respond.
- [ ] CI green on `main`.

---

## Escalation
- Toolchain (Windows/ARM) issues → pinned images + documented prerequisites; ask before working around.
- Package incompatibility with net10.0 → report before substituting alternatives.
