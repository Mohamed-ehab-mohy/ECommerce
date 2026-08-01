# Document 37 — Coding Standards & Conventions

> **Platform:** E-Commerce Platform (Working Title: `ECommerce`)
> **Document Type:** Engineering Standard
> **Status:** Draft v1.0 for review
> **Audience:** All Developers, Reviewers, Tech Lead
> **Inputs:** `06-system-architecture.md`, `06a-domain-model.md`, `30-test-strategy-and-quality-gates.md`
> **Relationship:** Defines how code is written and reviewed so the codebase stays consistent, reviewable, and maintainable. Enforced via `.editorconfig`, analyzers, and CI gates (`31`).

---

## 1. Document Control

| Version | Date       | Author / Owner | Change Summary                |
|---------|------------|----------------|--------------------------------|
| 0.1     | 2026-07-20 | Tech Lead      | Naming, style, C# conventions |
| 0.2     | 2026-07-28 | Tech Lead      | DDD/EF/API/logging conventions |
| 1.0     | 2026-07-31 | Tech Lead      | Baseline release              |

### 1.1 Approvals

| Role                | Name | Decision | Date |
|----------------------|------|----------|------|
| Technical Lead       | —    | —        | —    |
| Enterprise Architect | —    | —        | —    |
| QA Lead              | —    | —        | —    |

---

## 2. Purpose & Scope

This document defines the **coding standards** for the platform: language and tooling versions, naming, project structure, C# idioms, DDD/EF/API/logging conventions, test conventions, review expectations, and git/commit practice.

Scope: all C# code (`ECommerce.slnx`), tests, migrations, and infrastructure-as-code relevant to application developers. Machine-enforced rules live in `.editorconfig` + analyzers and are non-negotiable in review.

---

## 3. Language, Runtime & Tooling

| Item | Standard |
|------|----------|
| Language | C# 13 (latest with .NET 10) |
| Target framework | `net10.0` |
| Nullable | Enabled project-wide; warnings as errors |
| Implicit usings | Enabled |
| File-scoped namespaces | Required |
| Analyzers | .NET SDK analyzers + StyleCop (opinionated subset) + custom architecture analyzers |
| Formatter | `dotnet format` enforced in CI |
| Warnings | All warnings treated as errors (production projects) |

---

## 4. Naming Conventions

### 4.1 Identifier Rules

| Identifier | Convention | Example |
|------------|------------|---------|
| Namespace | PascalCase, single-company prefix | `ECommerce.Domain` |
| Class / record / enum / interface | PascalCase (interface `I` prefix) | `OrderAggregate`, `IOrderRepository` |
| Methods / local functions | PascalCase | `PlaceOrder()` |
| Public members / properties | PascalCase | `TotalAmount` |
| Private fields | `_camelCase` | `_orderRepository` |
| Constants | PascalCase | `MaxRetryCount` |
| Local variables / parameters | camelCase | `orderNumber` |
| Type parameters | PascalCase `T` | `TEntity` |
| Async methods | `Async` suffix | `GetAsync()` |
| Money / currency | Never bare `decimal` in public API | `Money`, `Price` value objects |
| Date/time | Never bare UTC ambiguity | suffix `At` (`PlacedAt`, `ExpiresAt`); `UtcNow` only |

### 4.2 File & Folder Conventions

- One primary type per file; filename matches type name (`OrderAggregate.cs`).
- Folders mirror namespaces; domain folders per aggregate (`Ordering/Orders`, `Ordering/OrderItems`).
- No `using` directives inside namespaces; `global using` for shared namespaces only via dedicated file.

---

## 5. Project Structure

### 5.1 Solution Layout (matches `ECommerce.slnx`)

```
ECommerce.Shared/         # Cross-layer shared kernel: Result/Error primitives only
ECommerce.Domain/         # Entities, value objects, aggregates, invariants, domain events, ports
ECommerce.UseCases/       # Application use-cases/handlers, DTOs, business orchestration
ECommerce.Infrastructure/ # EF Core, repos, HTTP clients, bus, background workers, integrations
ECommerce.Api/            # Controllers/endpoints, middleware, OpenAPI, SignalR hubs
ECommerce.UnitTests/
ECommerce.ArchitectureTests/
```

### 5.2 Reference Rules (enforced by ArchitectureTests)

| Project | May reference |
|---------|---------------|
| `Shared` | .NET only — zero dependencies |
| `Domain` | `Shared` |
| `UseCases` | `Domain`, `Shared` |
| `Infrastructure` | `Domain`, `UseCases`, `Shared` |
| `Api` | `UseCases`, `Infrastructure`, `Shared` |
| Tests | Their target + shared test libs |

Forbidden: `DbContext` in API, DTOs in Domain, cycles, `Infrastructure` in `UseCases`.

---

## 6. C# Coding Conventions

### 6.1 Idioms

| Rule | Detail |
|------|--------|
| Records for DTOs/immutable values | `public sealed record OrderLineDto(...)` |
| Sealed by default | `sealed` unless designed for inheritance |
| Pattern matching preferred | `is`, `switch` expressions over `if/else` chains |
| Nullable handling | No `!` unless proven invariant; avoid `.Value` on `Nullable<T>` in logic |
| LINQ | Method syntax; no deferred enumeration surprises (`ToList()` at boundaries) |
| String building | `StringBuilder` for loops; interpolated strings otherwise |
| `var` | Used when type is evident; explicit type when it aids readability |
| Exceptions | Only for exceptional flow; no exceptions for validation |

### 6.2 Async/Await

| Rule | Detail |
|------|--------|
| Async all the way | No `.Result` / `.Wait()` / `async void` (except event handlers) |
| ConfigureAwait | `ConfigureAwait(false)` in libraries (Infrastructure), not needed in app code |
| Cancellation | Thread `CancellationToken` through all async boundaries; honor on shutdown |
| Parallelism | `Task.WhenAll` for independent; no blind `Parallel.ForEach` on shared state |

### 6.3 Error Handling

| Rule | Detail |
|------|--------|
| Domain failures | Domain exceptions or result objects for business rules (see `06a-domain-model.md`) |
| Validation | FluentValidation at API boundary; never inside Domain |
| Infrastructure errors | Wrap + rethrow with context; never swallow; log via logger, not `Console` |
| Logging | Structured (`logger.LogError("... {OrderId}", orderId)`), never string interpolation |

---

## 7. DDD Conventions (Domain)

| Rule | Detail |
|------|--------|
| Aggregates | Rich models with encapsulated state; behavior via methods (`order.Cancel(reason)`) |
| Value objects | `sealed record`, immutable, equality by value, factory + validation (`Money`, `Sku`, `OrderNumber`) |
| Entities | Identity via value object id (`OrderId`, `CustomerId`) |
| Domain events | `DomainEvent` records; raised by aggregates, dispatched via outbox |
| Repositories | Ports in Domain (`IOrderRepository`), implementations in Infrastructure |
| No persistence in Domain | No EF attributes/navigation polluting Domain entities; mapping in Infrastructure |

---

## 8. EF Core Conventions

| Rule | Detail |
|------|--------|
| Configurations | `IEntityTypeConfiguration<T>` per aggregate; applied via `ApplyConfigurationsFromAssembly` |
| Naming | `snake_case` tables/columns; singular table names |
| Keys | Surrogate `long`/`Guid` PKs + natural unique indexes (`order_number`, `sku`) |
| Money | `decimal(18,4)` conversion on value objects (see `07-data-model-erd.md`) |
| Enums | Stored as string by default (readability); ints only with explicit mapping |
| Soft delete | `IsDeleted` + query filter where required (customers, products) |
| Concurrency | `xmin`/rowversion for optimistic concurrency where specified |
| Migrations | Additive expand-contract only; no destructive auto-generate without review |
| Raw SQL | Never string-concatenated; parameterized or in validated views only |

---

## 9. API Conventions (matches `08-api-design.md`)

| Rule | Detail |
|------|--------|
| Controllers | Minimal endpoints preferred for simple ops; controllers for complex resources |
| REST | Nouns, correct verbs, plural collection routes `/api/v1/orders` |
| DTOs | Request/response DTOs in `UseCases`; no Domain entities exposed |
| Mapping | Mapster configured centrally; manual mapping only when logic required |
| Errors | RFC 9457 ProblemDetails via middleware; no raw exceptions to client |
| Pagination | Consistent `page/pageSize` + `X-Total-Count`; caps enforced |
| Authz | `[Authorize(Policy = "...")]`; permission codes central constants |
| Validation | FluentValidation validators referenced by endpoints; request models thin |
| Idempotency | `Idempotency-Key` honored on specified endpoints |

---

## 10. Dependency Injection & Configuration

| Rule | Detail |
|------|--------|
| Composition root | DI registration in `Infrastructure`/`Api` extension methods (`AddECommerceInfrastructure`) |
| Scopes | Scoped = per request/use-case; transient for stateless; singleton only for stateless thread-safe |
| Configuration | Strongly-typed options classes (`IOptions<T>`), never `IConfiguration` scattered |
| Secrets | Never in config files; env/Vault only |
| HttpClient | `IHttpClientFactory` + typed clients, `Polly`/timeouts configured |

---

## 11. Background Work & Messaging

| Rule | Detail |
|------|--------|
| Consumers | One consumer per message type; idempotent handling; poison-message handling |
| Outbox/Inbox | Writes through outbox pattern; inbox dedupe (`idempotency_key`) |
| Scheduling | Hangfire jobs in `worker-reporting`; jobs idempotent + retryable |
| Queue names | `ecommerce.{context}.{topic}` naming |

---

## 12. Test Conventions (matches `30-test-strategy-and-quality-gates.md`)

| Rule | Detail |
|------|--------|
| Frameworks | xUnit; `FluentAssertions`; NSubstitute |
| Naming | `Method_Scenario_ExpectedBehavior` |
| Traits | `[Trait("FRS","F-14.2")]` for traceability |
| Data | Builders (TestDataBuilder); no shared mutable fixtures |
| Assertions | One behavior focus per test; AAA layout |
| Integration | Testcontainers; per-fixture DB; no wall-clock asserts |
| Coverage | Domain/pricing/inventory ≥ 80%; overall ≥ 70% |

---

## 13. Git & Commit Conventions

| Aspect | Standard |
|--------|----------|
| Branch naming | `feature/{short-slug}`, `hotfix/{slug}`, `docs/{slug}` |
| Commits | Conventional Commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:` |
| PR size | ≤ 400 changed lines preferred; split large changes |
| PR title | Conventional Commits style; linked to issue |
| Review | 1 approval (2 for `/src`); author responds to all comments |
| History | Squash merge to `main`; linear history |

---

## 14. Enforcement & Quality Gates

| Gate | Enforcement |
|------|-------------|
| `.editorconfig` + analyzers | `dotnet format` + build warnings-as-errors in CI (G1) |
| Architecture rules | `ECommerce.ArchitectureTests` in CI (G1) |
| Style review | PR review checklist (naming, async, DDD, EF, API rules) |
| Coverage | Gate in CI (G2) per `30` |
| Deviations | Only via documented exception (ADR-style note in PR); never silent |

---

## 15. Approvals

| Role                | Name | Decision | Date |
|----------------------|------|----------|------|
| Technical Lead       | —    | —        | —    |
| Enterprise Architect | —    | —        | —    |
| QA Lead              | —    | —        | —    |

---

*End of Document 37 — Coding Standards & Conventions.*
*Next document on request: `02-glossary-and-definitions.md` (or any other roadmap item).*
