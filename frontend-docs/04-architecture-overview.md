# 04 - Architecture Overview

As a frontend developer, you don't need to know the deep internals of the backend, but understanding the high-level architecture helps in debugging and designing robust frontend features.

## Tech Stack
- **API Framework:** ASP.NET Core 10 (C#)
- **Database:** PostgreSQL
- **Caching & Distributed Locks:** Redis
- **Message Broker:** RabbitMQ
- **Background Jobs:** Hangfire
- **Search (Optional):** Elasticsearch

## Key Concepts

### 1. CQRS (Command Query Responsibility Segregation)
The backend separates read operations (Queries) from write operations (Commands).
- **Queries** (e.g., fetching a product list) are fast and often cached.
- **Commands** (e.g., placing an order) might be processed asynchronously.

### 2. Idempotency
Certain endpoints (like Payments or Order Placement) are idempotent. If the frontend encounters a network timeout and retries the exact same request with the same `Idempotency-Key` header, the backend will ensure the operation is only processed once.

### 3. Outbox Pattern & Background Processing
Some actions (like sending emails or publishing events) don't happen immediately during the HTTP request. They are saved to an "Outbox" and processed in the background. If you place an order, the immediate HTTP response is just an acknowledgement, while the actual payment processing might happen seconds later. This is why you should listen to **SignalR WebSockets** (see `03-real-time-events.md`) to get the final status.

### 4. Rate Limiting
The API enforces rate limits to prevent abuse (e.g., 50 requests per 60 seconds). Crucially, these limits are **Tenant-Based**; they are tied to the active subscription plan of the tenant. If you send too many requests, you will receive a `429 Too Many Requests` response. Ensure your frontend handles this gracefully by implementing retry logic with exponential backoff.

### 5. SaaS & Multi-Tenancy
This application acts as a SaaS platform. All data is scoped to a specific `TenantId`. As a frontend developer:
- Some API endpoints may require you to pass a custom header or domain to identify the tenant.
- Billing is handled via Stripe Webhooks natively in the backend (`/api/v1/webhooks/stripe`).
- Trial expirations and plan downgrades happen automatically in the background using Hangfire.
- SSL and custom domains are handled by the infrastructure via Traefik.
