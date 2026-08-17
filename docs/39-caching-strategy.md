# Document 39 — Caching Strategy

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** Caching Architecture & Patterns
> **Status:** Draft v1.0
> **Audience:** Engineering, DevOps

---

## 1. Overview

The platform implements a **two-tier caching architecture**:

- **L1 (In-Memory):** Per-process caches for high-frequency, low-latency access patterns.
- **L2 (Redis):** Distributed cache shared across application instances for cross-process state.

Redis is connected via **StackExchange.Redis** with a single `IConnectionMultiplexer` singleton shared across all consumers. The L1 layer complements Redis for scenarios where distributed consistency is not required but sub-millisecond reads are critical.

---

## 2. L1 — In-Memory Caches

### 2.1 InMemoryShippingRateCache

**File:** `Infrastructure/Shipping/InMemoryShippingRateCache.cs`

Caches carrier shipping quotes to avoid repeated external API calls during checkout.

| Property | Value |
|----------|-------|
| Backing store | `ConcurrentDictionary<string, Entry>` |
| TTL | 10 minutes (`DefaultTtl`) |
| Key | Carrier quote lookup key |
| Thread safety | Lock-free via `ConcurrentDictionary` |
| Expiration | Lazy eviction on read; expired entries are removed on access |

The cache uses `TimeProvider` (not `DateTime.UtcNow`) for testability. Entries store the `CarrierQuoteResult` and an `ExpiresAtUtc` timestamp.

### 2.2 InMemoryNotificationTemplateStore

**File:** `Infrastructure/Notifications/InMemoryNotificationTemplateStore.cs`

An in-memory store of notification templates with locale fallback. Templates are loaded once at construction and never expire.

| Property | Value |
|----------|-------|
| Backing store | `IReadOnlyDictionary<string, TemplateDefinition>` |
| Locale fallback chain | `["en", "ar"]` |
| Template format | Subject + HTML body with `{placeholder}` substitution |
| Template keys | `order.confirmation`, `order.shipped`, `order.cancelled`, `low.stock.alert`, `integrations.webhook.suspended` |

Template resolution order:
1. Exact match: `{templateKey}.{locale}` (e.g., `order.confirmation.en`)
2. Fallback chain: Iterate `["en", "ar"]` for the first match
3. Throws `KeyNotFoundException` if no match found

### 2.3 InMemoryLoginAttemptThrottler

**File:** `UseCases/Identity/InMemoryLoginAttemptThrottler.cs`

Tracks failed login attempts per client IP address for rate-limiting.

| Property | Value |
|----------|-------|
| Backing store | `Dictionary<string, (int Count, DateTime WindowStartUtc)>` |
| Window | Configurable via `AuthSettings.LoginAttemptWindowMinutes` |
| Threshold | Configurable via `AuthSettings.MaxFailedLoginAttemptsPerIp` |
| Thread safety | Manual `lock` synchronization |
| Reset | On successful login (`RecordSuccess`) or window expiry |

The throttler exposes three operations:
- `IsBlocked(clientIp, utcNow, out retryAfterSeconds)` — checks if the IP is currently throttled
- `RecordFailure(clientIp, utcNow)` — increments the failure counter
- `RecordSuccess(clientIp, utcNow)` — removes the IP from tracking

---

## 3. L2 — Redis Distributed Cache

### 3.1 Connection Setup

A single `IConnectionMultiplexer` is created from the `Redis` connection string and registered as a singleton in `Infrastructure.DependencyInjection.cs:92`:

```csharp
services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(redisConnectionString));
```

### 3.2 Shopping Cart

**File:** `Infrastructure/Carts/CartRepository.cs`, `CartCacheCodec.cs`

Cart state is stored in Redis as serialized JSON with a custom codec for domain model hydration.

| Property | Value |
|----------|-------|
| Cache key | `cart:{ownerKey}` |
| TTL | 30 days |
| Serialization | `CartCacheCodec` (JSON via `System.Text.Json`) |
| Stampede lock | `cart:{ownerKey}:lock` (100 ms TTL) |

`CartCacheCodec` maintains dedicated DTOs (`CartCacheDto`, `CartCacheItemDto`) for serialization, keeping the domain model clean of cache concerns.

### 3.3 Feature Flags

**File:** `Infrastructure/Flags/CachedFeatureFlagService.cs`

Feature flags are cached in Redis to avoid hitting the database on every evaluation call.

| Property | Value |
|----------|-------|
| Cache key | `feature-flag:{key}` |
| TTL | 30 seconds |
| Serialization | `System.Text.Json` with `JsonSerializerDefaults.Web` |
| Fallback | On Redis failure, falls back to `IFeatureFlagRepository` directly |

The service uses a read-through pattern: on cache miss, it fetches from the repository and populates the cache. Redis failures are logged as warnings and do not block flag evaluation.

### 3.4 SignalR Backplane

Redis is configured as the SignalR backplane for horizontal scaling of real-time event delivery.

| Property | Value |
|----------|-------|
| Channel prefix | `signalr:` |
| Protocol | Redis pub/sub |
| Registration | `AddStackExchangeRedis()` in `API/DependencyInjection.cs:54` |

### 3.5 OutboxMetrics

**File:** `Infrastructure/Messaging/OutboxMetrics.cs`

`OutboxMetrics` is an in-memory singleton (not cached in Redis) that tracks outbox processing telemetry using `System.Diagnostics.Metrics`:

| Metric | Type | Description |
|--------|------|-------------|
| `outbox.messages.published` | Counter | Messages successfully published |
| `outbox.messages.dead_lettered` | Counter | Messages moved to dead letter |
| `outbox.lag_seconds` | Gauge | Seconds behind the oldest unprocessed message |

Registered as `services.AddSingleton<OutboxMetrics>()` in `DependencyInjection.cs:155`.

---

## 4. Cache Invalidation Patterns

### 4.1 Write-Through

Cart updates are written to Redis immediately after the domain model is updated, ensuring consistency between the application state and the cache.

### 4.2 TTL-Based Expiration

| Cache | TTL | Rationale |
|-------|-----|-----------|
| Shipping rates | 10 minutes | Carrier quotes change infrequently |
| Feature flags | 30 seconds | Allows near-real-time flag toggling |
| Shopping cart | 30 days | Long-lived session for guest-to-auth merge |
| Notification templates | None (static) | Loaded once, never expire in-memory |

### 4.3 Event-Driven Invalidation

Feature flag updates invalidate the Redis cache on write. When an admin updates a flag via the API, the `CachedFeatureFlagService` should evict the corresponding cache key (or let the 30-second TTL expire naturally).

### 4.4 Lazy Eviction

The `InMemoryShippingRateCache` uses lazy eviction: expired entries are detected and removed only when accessed. This avoids background cleanup threads while keeping the cache bounded.

---

## 5. Redis Health Check

**File:** `Infrastructure/Redis/RedisHealthCheck.cs`

A custom `IHealthCheck` implementation pings the Redis database:

```csharp
var database = multiplexer.GetDatabase();
await database.PingAsync();
return HealthCheckResult.Healthy();
```

On failure, returns `HealthCheckResult.Unhealthy("Redis is not reachable.", exception)`. This integrates with the ASP.NET Core health check middleware for `/health` endpoint reporting.

---

## 6. Configuration Reference

| Setting | Source | Default | Description |
|---------|--------|---------|-------------|
| `ConnectionStrings:Redis` | `appsettings.json` / env | — | Redis connection string |
| `ConnectionStrings:Postgres` | `appsettings.json` / env | — | PostgreSQL connection string |
| `Database:MigrationStartupMaxAttempts` | `appsettings.json` | 40 | Migration retry count on startup |
| `AuthSettings:MaxFailedLoginAttemptsPerIp` | `appsettings.json` | — | Login throttle threshold |
| `AuthSettings:LoginAttemptWindowMinutes` | `appsettings.json` | — | Login throttle window |

### 6.1 Environment Variables

| Variable | Maps To |
|----------|---------|
| `ConnectionStrings__Redis` | Redis connection string |
| `ConnectionStrings__Postgres` | PostgreSQL connection string |
| `ConnectionStrings__SqlServer` | SQL Server connection string (optional) |
| `Database__DataProvider` | `"Postgres"` or `"SqlServer"` |

### 6.2 Redis Key Namespace

| Prefix | Consumer | TTL |
|--------|----------|-----|
| `cart:{ownerKey}` | `CartRepository` | 30 days |
| `cart:{ownerKey}:lock` | `CartRepository` | 100 ms |
| `feature-flag:{key}` | `CachedFeatureFlagService` | 30 seconds |
| `signalr:` | SignalR backplane | N/A (pub/sub) |
