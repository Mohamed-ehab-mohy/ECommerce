# E-Commerce Enterprise Backend Architecture

<div align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white" alt="Redis" />
  <img src="https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white" alt="RabbitMQ" />
  <img src="https://img.shields.io/badge/Docker-2CA5E0?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  <img src="https://img.shields.io/badge/GraphQL-E10098?style=for-the-badge&logo=graphql&logoColor=white" alt="GraphQL" />
</div>

<br/>

## Executive Summary

A production-grade, highly scalable e-commerce backend system built with **.NET 10**. This repository represents a complete microservices-ready monolithic architecture, engineered to demonstrate advanced enterprise-level software design patterns. The system is highly decoupled, fault-tolerant, and designed to handle high-throughput concurrent operations without data corruption or state inconsistency.

---

## Architecture & Applied Design Patterns

This system strictly adheres to the following architectural principles and patterns to ensure high maintainability, testability, and scalability:

- **Clean Architecture (Onion Architecture):** Strict separation of concerns divided into Domain, Use Cases (Application), Infrastructure, and API Presentation layers. Dependencies point inwards.
- **Domain-Driven Design (DDD):** Rich, encapsulated domain models utilizing Aggregate Roots, Value Objects, Entities, and Domain Events to represent complex business logic.
- **CQRS (Command Query Responsibility Segregation):** Mediated via MediatR. Write operations (Commands) are fully separated from Read operations (Queries), allowing for independent scaling and optimization.
- **Event-Driven Architecture:** Asynchronous inter-module communication using MassTransit and RabbitMQ to decouple services and improve system resilience.
- **Transactional Outbox Pattern:** Guarantees "at-least-once" delivery of messages to the message broker. Domain events are atomically saved with business transactions to the database and later published asynchronously by a background sweeper.
- **Idempotency:** Critical mutating endpoints (such as Payment Processing and Order Placement) implement Idempotency-Keys to safely handle network retries and prevent duplicate transactions.
- **Optimistic Concurrency Control:** Implemented via Entity Framework Core `RowVersion` tokens to prevent race conditions during high-frequency wallet deposits/withdrawals and inventory decrements.
- **Distributed Locking:** Utilizes Redis distributed locks (RedLock algorithm) to serialize access to highly contested resources (e.g., reserving stock for a specific product variant).
- **Soft Deletion Mechanism:** Global EF Core query filters and interceptors ensure records are never hard-deleted, maintaining referential integrity and audit trails.

---

## Technical Stack & Infrastructure

- **Framework:** .NET 10.0 (ASP.NET Core Web API)
- **Language:** C# 13
- **ORM:** Entity Framework Core (Code-First Migrations)
- **Primary Database:** PostgreSQL (Relational Data)
- **Caching & Locks:** Redis (Distributed Caching, Session State, Distributed Locking)
- **Message Broker:** RabbitMQ (Asynchronous Messaging)
- **Service Bus Wrapper:** MassTransit
- **Background Jobs:** Hangfire (Persistent Scheduled Tasks)
- **GraphQL Engine:** HotChocolate
- **Real-Time Communication:** ASP.NET Core SignalR (WebSockets)
- **Logging:** Serilog (Structured JSON Logging)
- **Observability:** OpenTelemetry, Prometheus, and Grafana (Metrics & Tracing)
- **Validation:** FluentValidation (Pipeline Behaviors)
- **Testing:** xUnit, Moq, FluentAssertions, Testcontainers, Respawn, k6 (Load Testing)

---

## Comprehensive Feature Set

### 1. Identity, Security & Access Management
- **JWT Bearer Authentication:** Secure stateless authentication.
- **Role-Based Access Control (RBAC):** Claims-based authorization policies for separating Customers and Administrators.
- **Password Security:** Hashes utilizing Argon2id / PBKDF2.
- **Rate Limiting:** Global and endpoint-specific rate limiting to prevent Brute-Force and DDoS attacks using ASP.NET Core RateLimiter.
- **Content-Security-Policy (CSP):** Hardened middleware rejecting inline scripts, cross-origin framing, and enforcing secure resource loading.

### 2. Catalog & Search Module
- Comprehensive management of Products, Brands, Categories, and Variants.
- **REST & GraphQL:** Dual API exposure. REST for standard management, and GraphQL for highly flexible, client-driven product queries, filtering, and pagination.
- **Caching Strategy:** Redis-backed caching for frequently accessed catalog read-models.

### 3. Shopping Cart & Checkout Workflow
- **Persistent Distributed Cart:** User shopping carts are stored in Redis for low-latency read/write access.
- **Complex Checkout Pipeline:** An orchestrated flow that handles validation, stock reservation, price calculation, payment execution, and order generation atomically.

### 4. Digital Wallets & Payments
- **Wallet System:** Users possess internal digital wallets allowing deposits, withdrawals, and direct transfers.
- **Concurrency Protection:** EF Core Concurrency Tokens ensure that simultaneous transactions cannot overdraw a wallet or corrupt the ledger.

### 5. Inventory & Warehouse Management
- Real-time stock tracking per product variant.
- **Distributed Locking:** Redis locks ensure that if two users attempt to buy the last item simultaneously, only one succeeds.
- Stock replenishment and low-stock alerts.

### 6. Order Management & Real-Time Tracking
- Comprehensive order state machine (Pending -> Payment Verified -> Processing -> Shipped -> Delivered).
- **SignalR Integration:** WebSockets push live order status updates directly to the frontend clients without requiring polling.

### 7. Background Processing & Scheduling
- **Hangfire Jobs:** Recurring and fire-and-forget jobs for tasks such as cleaning up abandoned carts, generating daily sales reports, and sending asynchronous email notifications.
- **Outbox Sweeper:** A hosted background service that polls the `OutboxMessages` table and publishes pending events to RabbitMQ.

---

## CI/CD Pipeline & Quality Gates

The repository features a fully automated, multi-stage GitHub Actions pipeline designed to enforce extreme code quality and system stability:

1. **Format Check:** Enforces strict code formatting via `dotnet format`.
2. **Static Code Analysis (Secret Scanning):** GitLeaks integration prevents accidental committing of sensitive API keys or credentials.
3. **Unit Tests:** High coverage of domain logic and use-case handlers using mocked dependencies.
4. **Architecture Tests:** Validates that Clean Architecture rules are not violated (e.g., Domain layer referencing Infrastructure).
5. **Integration Tests:** Utilizes Testcontainers to spin up real instances of PostgreSQL and Redis to test database interactions and API endpoints.
6. **Load & Performance Testing:** Uses k6 to execute smoke load tests against the API within the pipeline to prevent performance regressions.

---

## Local Development Environment

The system is configured for a frictionless developer experience using Docker Compose.

### Requirements
- Docker Desktop
- .NET 10 SDK

### Startup Instructions

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Spin up the infrastructure dependencies:
   ```bash
   docker compose up -d postgres redis rabbitmq
   ```
3. Run the application:
   ```bash
   dotnet run --project src/ECommerce.API
   ```

### API Documentation & Diagnostics
- **Swagger UI (REST APIs):** `http://localhost:5139/swagger`
- **GraphQL IDE (Banana Cake Pop):** `http://localhost:5139/graphql`
- **Hangfire Dashboard:** `http://localhost:5139/hangfire`
