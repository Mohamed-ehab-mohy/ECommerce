# Document 33 — Architecture Decision Records

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** Architecture Decision Records (ADR) Index
> **Status:** Draft v1.0
> **Audience:** Engineering, Architecture

---

## 1. Overview

This document catalogs the key Architecture Decision Records for the ECommerce platform. Each ADR follows the standard format: **Title**, **Status**, **Context**, **Decision**, **Consequences**. ADRs are referenced throughout other design documents (e.g., `docs/06c-bounded-contexts.md:§10`).

---

## 2. ADR Format

Each ADR captures a single architectural decision with:

- **Title**: Short noun phrase (e.g., "Use Clean Architecture with DDD")
- **Status**: Proposed / Accepted / Superseded
- **Context**: Forces at play, constraints, requirements
- **Decision**: What was decided and why
- **Consequences**: Positive and negative outcomes

---

## 3. ADR Index

### ADR-001: Clean Architecture with DDD

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | The platform requires clear separation of domain logic from infrastructure concerns to enable testability and independent evolution of bounded contexts. |
| **Decision** | Adopt Clean Architecture with four layers: `Domain` → `UseCases` → `Infrastructure` → `API`. Domain entities contain no infrastructure dependencies. Use cases orchestrate domain logic via port/adapter interfaces. |
| **Consequences** | Domain is fully unit-testable; infrastructure can be swapped (e.g., different databases, message brokers). Adds structural overhead but prevents coupling drift. |
| **References** | `src/ECommerce.Domain/`, `src/ECommerce.UseCases/`, `src/ECommerce.Infrastructure/`, `src/ECommerce.API/` |

---

### ADR-002: MediatR for CQRS

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | Commands and queries need clear separation with cross-cutting concerns (authorization, validation) applied uniformly. |
| **Decision** | Use MediatR 14.2.0 as the in-process mediator for CQRS. Commands and queries implement `IRequest<T>`. Handlers implement `IRequestHandler<T>`. Pipeline behaviors handle `AuthorizationBehavior<,>`. FluentValidation validators are auto-registered. |
| **Consequences** | Uniform request pipeline; easy to add cross-cutting behaviors. Handlers are in `ECommerce.UseCases/*/Handlers/`. All 20+ controllers dispatch through `IMediator`. |
| **References** | `src/ECommerce.UseCases/DependencyInjection.cs:25–29`, `src/ECommerce.UseCases/ECommerce.UseCases.csproj` |

---

### ADR-003: MassTransit v8.5.10 (Apache-2.0, Last Free Version)

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | Cross-context event publishing requires a reliable message bus abstraction. MassTransit changed to a commercial license after v8.5.10. |
| **Decision** | Pin MassTransit to v8.5.10 (Apache-2.0 licensed) for `MassTransit.Abstractions` and `MassTransit.RabbitMQ`. Use `IPublishEndpoint` for outbound publishing via the outbox. |
| **Consequences** | Free to use commercially; no license fee. Risk of falling behind on bug fixes/security patches in later versions. Consumers (`OrderPlacedConsumer`, `OrderCancelledConsumer`, `OrderShippedConsumer`) are registered in `Infrastructure/Messaging/DependencyInjection.cs`. |
| **References** | `src/ECommerce.Infrastructure/Messaging/DependencyInjection.cs:22–40`, `src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj`, `src/ECommerce.UseCases/ECommerce.UseCases.csproj` |

---

### ADR-004: PostgreSQL as Primary Database

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | The platform needs a single relational database for a modular monolith with schema-per-context isolation. |
| **Decision** | Use PostgreSQL with Npgsql (dynamic JSON support). Single database, one schema per bounded context (`identity`, `catalog`, `ordering`, `inventory`, etc.). Cross-schema access only through published contracts. |
| **Consequences** | Simplified deployment (one database); schema isolation provides logical separation without microservice overhead. Requires discipline to prevent cross-schema joins. Referenced in `docs/06c-bounded-contexts.md:§7`. |
| **References** | `src/ECommerce.Infrastructure/DependencyInjection.cs:70–76`, `src/ECommerce.API/appsettings.Development.json:9` |

---

### ADR-005: Redis for Caching

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | Distributed cache needed for session data, shopping cart read performance, feature flag evaluation, and SignalR backplane. |
| **Decision** | Use StackExchange.Redis 3.1.13 via a singleton `IConnectionMultiplexer`. Cache patterns: read-through with stampede protection (carts), TTL-based (feature flags), pub/sub backplane (SignalR). |
| **Consequences** | Sub-millisecond reads for hot paths (carts, flags). Requires Redis availability for feature flag evaluation (graceful fallback implemented). SignalR backplane enables horizontal scaling. |
| **References** | `src/ECommerce.Infrastructure/DependencyInjection.cs:78`, `src/ECommerce.Infrastructure/Carts/CartRepository.cs`, `src/ECommerce.Infrastructure/Flags/CachedFeatureFlagService.cs` |

---

### ADR-006: RabbitMQ for Message Transport

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | Reliable asynchronous event delivery is needed for outbox-based eventual consistency between bounded contexts. |
| **Decision** | Use RabbitMQ as the transport for MassTransit. Configure quorum queues for durability. Connection string via `ConnectionStrings:RabbitMq`. Gracefully disabled when no connection string is configured (`DependencyInjection.cs:17–19`). |
| **Consequences** | Durable message delivery; quorum queues survive broker restarts. Optional: system functions without RabbitMQ (in-process dispatch only) for development simplicity. |
| **References** | `src/ECommerce.Infrastructure/Messaging/DependencyInjection.cs:16–39`, `src/ECommerce.API/appsettings.Development.json:11` |

---

### ADR-007: xUnit for Testing

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | The project requires a test framework for unit, integration, and architecture tests. |
| **Decision** | Use xUnit across all three test projects: `ECommerce.UnitTests`, `ECommerce.IntegrationTests`, `ECommerce.ArchitectureTests`. Architecture tests enforce cross-context reference boundaries. |
| **Consequences** | Consistent testing approach; xUnit's parallel execution and async-friendly design. Architecture tests catch boundary violations at CI time. |
| **References** | `tests/ECommerce.UnitTests/`, `tests/ECommerce.IntegrationTests/`, `tests/ECommerce.ArchitectureTests/` |

---

### ADR-008: EF Core Code First with Append-Only Stock Movements

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | Stock ledger must maintain a complete audit trail; current stock levels are derived from movements. |
| **Decision** | Use EF Core Code First with migrations. Stock movements (`stock_movements` table) are append-only. `StockItem` current quantity is derived from summing movements. Domain events are captured via `DomainEventsInterceptor` during `SaveChanges`. |
| **Consequences** | Complete audit trail for inventory changes; stock can be reconstructed from any point in time. Requires careful migration management. Prevents accidental stock data mutations. |
| **References** | `src/ECommerce.Infrastructure/Outbox/DomainEventsInterceptor.cs`, `src/ECommerce.Infrastructure/Data/`, `docs/06c-bounded-contexts.md:§4.6` |

---

### ADR-009: Outbox Pattern for Reliable Event Publishing

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | Domain events must be published reliably without losing messages or duplicating side effects. |
| **Decision** | Implement the Transactional Outbox pattern: domain events are persisted to `outbox_events` table within the same transaction as business data (`DomainEventsInterceptor`). A polling `OutboxBackgroundService` publishes to the message bus and in-process handlers. Uses `FOR UPDATE SKIP LOCKED` for concurrent-safe polling. Max 5 retries before dead-lettering. |
| **Consequences** | At-least-once delivery guarantee; zero message loss on DB commit. Consumers must be idempotent. Observable via `OutboxMetrics` (published count, dead-letter count, lag gauge). |
| **References** | `src/ECommerce.Infrastructure/Outbox/DomainEventsInterceptor.cs`, `src/ECommerce.Infrastructure/Outbox/OutboxBackgroundService.cs`, `src/ECommerce.Infrastructure/Messaging/OutboxPublisher.cs`, `src/ECommerce.Infrastructure/Messaging/OutboxMetrics.cs` |

---

### ADR-010: FluentValidation for Request Validation

| Field | Value |
|-------|-------|
| **Status** | Accepted |
| **Context** | Input validation must be consistent, testable, and separated from handler logic. |
| **Decision** | Use FluentValidation 12.1.1 with `FluentValidation.DependencyInjectionExtensions`. Validators are auto-registered via `AddValidatorsFromAssembly`. Each command/query has a corresponding `AbstractValidator<T>`. Validation runs before handler execution via the MediatR pipeline. |
| **Consequences** | Declarative, testable validation rules; consistent error responses via `ValidationErrors` helper. Validators live alongside commands/queries in `ECommerce.UseCases/*/Commands/` and `Queries/`. |
| **References** | `src/ECommerce.UseCases/DependencyInjection.cs:31`, `src/ECommerce.UseCases/ECommerce.UseCases.csproj:9–10` |

---

## 4. Cross-References

| ADR | Referenced By |
|-----|--------------|
| ADR-004 | `docs/06c-bounded-contexts.md:§7` (data ownership), `§10` (decisions table) |
| ADR-003 | `docs/06c-bounded-contexts.md:§6` (consistency strategy), `§10` |
| ADR-005 | `docs/06c-bounded-contexts.md:§10` (Shared Kernel for Inventory↔Ordering) |

---

## 5. File References

| File | Path |
|------|------|
| Domain events interceptor | `src/ECommerce.Infrastructure/Outbox/DomainEventsInterceptor.cs` |
| Outbox background service | `src/ECommerce.Infrastructure/Outbox/OutboxBackgroundService.cs` |
| Outbox publisher | `src/ECommerce.Infrastructure/Messaging/OutboxPublisher.cs` |
| Outbox metrics | `src/ECommerce.Infrastructure/Messaging/OutboxMetrics.cs` |
| MassTransit DI | `src/ECommerce.Infrastructure/Messaging/DependencyInjection.cs` |
| UseCases DI (MediatR, FluentValidation) | `src/ECommerce.UseCases/DependencyInjection.cs` |
| Infrastructure DI (EF Core, Redis) | `src/ECommerce.Infrastructure/DependencyInjection.cs` |
| API DI (SignalR, auth) | `src/ECommerce.API/DependencyInjection.cs` |
| Bounded contexts (ADR cross-refs) | `docs/06c-bounded-contexts.md:§10` |
