# 🚀 E-Commerce Enterprise Backend (Microservices Ready)

<div align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white" alt="Postgres" />
  <img src="https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white" alt="Redis" />
  <img src="https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white" alt="RabbitMQ" />
  <img src="https://img.shields.io/badge/Docker-2CA5E0?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  <img src="https://img.shields.io/badge/GraphQL-E10098?style=for-the-badge&logo=graphql&logoColor=white" alt="GraphQL" />
</div>

<br/>

A production-grade, highly scalable e-commerce backend built with **.NET 10**, strictly adhering to **Clean Architecture**, **Domain-Driven Design (DDD)**, and **CQRS** patterns. Engineered for high performance, fault tolerance, and eventual consistency using the **Outbox Pattern** and **Event-Driven Architecture**.

---

## 🏗️ System Architecture & Design Patterns

The system is designed with enterprise-level patterns to ensure modularity, testability, and scalability.

- **Clean Architecture:** Strict separation of concerns (Domain, UseCases, Infrastructure, API). The core business logic is completely isolated from external frameworks.
- **Domain-Driven Design (DDD):** Rich domain models with encapsulated logic, Aggregate Roots, Value Objects, and Domain Events.
- **CQRS (Command Query Responsibility Segregation):** Mediated via **MediatR**. Write operations (Commands) are fully separated from Read operations (Queries).
- **Event-Driven Architecture:** Asynchronous communication using **RabbitMQ** (via **MassTransit**) for decoupling services.
- **Transactional Outbox Pattern:** Ensures reliable event publishing and eventual consistency between the database and the message broker. 
- **Idempotency:** Implemented on critical endpoints (e.g., Payments, Order Processing) to safely handle retries and network failures.

---

## 🛠️ Tech Stack & Tooling

### **Core Stack**
- **Framework:** .NET 10.0 (ASP.NET Core Web API)
- **Language:** C# 13
- **ORM:** Entity Framework Core (Code-First)
- **Database:** PostgreSQL
- **Caching & Distributed Locking:** Redis
- **Message Broker:** RabbitMQ (with MassTransit)
- **Background Jobs:** Hangfire

### **Libraries & Nuget Packages**
- **MediatR:** For CQRS implementation.
- **FluentValidation:** For strict input validation pipeline behaviors.
- **HotChocolate:** For the GraphQL API endpoint.
- **SignalR:** For Real-Time WebSockets (e.g., Live Order Tracking).
- **YARP:** Reverse Proxy & API Gateway configuration.
- **Serilog:** Structured logging.
- **OpenTelemetry / Prometheus / Grafana:** For full system observability, metrics, and tracing.

---

## 🔐 Security & Authentication

Security is deeply integrated at multiple layers of the application:
1. **Identity & Access Management:** 
   - JWT (JSON Web Token) Bearer authentication.
   - Role-Based Access Control (RBAC) and Claims-based authorization policies.
2. **Data Protection:** 
   - Hashing passwords using secure modern algorithms (Argon2 / PBKDF2).
   - Sensitive PII masking in logs.
3. **Application Security:**
   - **Content-Security-Policy (CSP):** Highly restrictive CSP headers dynamically applied (relaxed only for Swagger/GraphQL UI).
   - Global Exception Handling to prevent stack trace leaks.
   - Rate Limiting via ASP.NET Core native Rate Limiter middleware to prevent DDoS & Brute-force attacks.

---

## 📦 Features Breakdown

### 1. 🛍️ Catalog & Inventory Management
- Full CRUD operations for Products, Categories, and Brands.
- **GraphQL Integration:** For complex filtering and dynamic fetching on the storefront.
- Real-time stock reservation and inventory decrementing using Redis Distributed Locks.

### 2. 🛒 Shopping Cart & Checkout
- Redis-backed persistent shopping cart.
- Complex checkout workflow ensuring atomicity (Order Creation -> Stock Reservation -> Payment).

### 3. 💳 Payments & Wallets
- User Digital Wallets with Transaction History (Deposit, Withdraw, Transfer).
- Concurrency control using EF Core RowVersions (Optimistic Concurrency) to prevent double-spending.

### 4. 📦 Order Management
- Order state machine processing (Pending -> Paid -> Shipped -> Delivered).
- Real-time order status updates pushed to the client via **SignalR**.

### 5. ⚙️ Background Processing
- **Hangfire** is used for scheduled tasks, cart abandonment emails, and database cleanup.
- **Outbox Processor** runs as a background hosted service to reliably push domain events to RabbitMQ.

---

## 🗄️ Database Design (High-Level ERD)

The database follows complete normalization (3NF) where required, but denormalizes specific read-models for performance.

- **Users/Identity:** `Users`, `Roles`, `UserRoles`, `Wallets`, `WalletTransactions`
- **Catalog:** `Products`, `Categories`, `Brands`, `ProductVariants`
- **Sales:** `Orders`, `OrderItems`, `Shipments`
- **Infrastructure:** `OutboxMessages` (for the Outbox pattern)

*All migrations are managed automatically through EF Core Migrations on startup.*

---

## 🚀 Getting Started

### 📂 Repository Structure
- `backend/`: Contains all C# source code, Dockerfiles, and configuration.
- `frontend-docs/`: Contains integration guides for Frontend Developers.

### 💻 Running Locally (Docker Compose)
This project is configured with a fully automated one-click local environment.

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Start the infrastructure (Postgres, Redis, RabbitMQ):
   ```bash
   docker compose up -d postgres redis rabbitmq
   ```
3. Run the API:
   ```bash
   dotnet run --project src/ECommerce.API
   ```

### 🌍 API Endpoints & UIs
- **Swagger UI (REST):** `http://localhost:5139/swagger`
- **GraphQL UI (Banana Cake Pop):** `http://localhost:5139/graphql`
- **Hangfire Dashboard:** `http://localhost:5139/hangfire`

---

## 🧪 CI/CD & Quality Gates

The repository is equipped with a highly robust **GitHub Actions** pipeline that runs on every Push/PR:
1. **Format Check:** Enforces strict coding standards (`dotnet format`).
2. **Static Code Analysis:** Secret scanning via GitLeaks.
3. **Automated Testing:** Runs full suites of Unit, Integration, and Architecture Tests.
4. **Load Testing (k6):** Spools up the Docker environment and runs a smoke load-test against the API to ensure no performance regressions.
