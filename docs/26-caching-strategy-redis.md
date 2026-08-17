# Document 26 — Caching Strategy (Redis)

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** Caching Architecture & Patterns
> **Status:** Draft v1.0
> **Audience:** Engineering, DevOps

---

## 1. Overview

The platform uses **Redis 3.1.13** (via StackExchange.Redis) as a distributed cache for session data, shopping carts, feature flag evaluation, and SignalR backplane. A single `IConnectionMultiplexer` singleton is registered at startup and shared across all consumers.

### Primary Use Cases

| Use Case | Cache Key Pattern | TTL | Source |
|----------|-------------------|-----|--------|
| Shopping Cart | `cart:{ownerKey}` | 30 days | `CartRepository.cs:14` |
| Feature Flag evaluation | `feature-flag:{key}` | 30 seconds | `CachedFeatureFlagService.cs:14`–`16` |
| SignalR backplane | Channel prefix `signalr:` | N/A (pub/sub) | `DependencyInjection.cs:47` |
| Stampede lock | `cart:{ownerKey}:lock` | 100 ms | `CartRepository.cs:12` |

---

## 2. Implementation

### 2.1 Connection Setup

A single `IConnectionMultiplexer` is created from the `Redis` connection string and registered as a singleton in `Infrastructure.DependencyInjection.cs:78`:

```csharp
services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(redisConnectionString));
```

The API layer passes the connection string from configuration at `API.DependencyInjection.cs:30`:

```csharp
configuration.GetConnectionString("Redis")!
```

### 2.2 Cart Caching (Read-Through with Stampede Protection)

`CartRepository` (`src/ECommerce.Infrastructure/Carts/CartRepository.cs`) implements a read-through cache with **lease-based stampede protection**:

1. **Cache hit**: `StringGetAsync` returns the serialized cart immediately (line 33).
2. **Cache miss**: A short-lived lock (`100 ms`) is acquired via `StringSetAsync` with `When.NotExists` (line 45). The winning request loads from PostgreSQL and populates the cache. Losers wait briefly, then retry the cache read (lines 65–70).
3. **Write-through**: On `SaveAsync`, after persisting to the database, the cache is immediately updated (line 136).
4. **Concurrency**: Optimistic concurrency via `Version` field; `CartConcurrencyException` thrown on mismatch (lines 101–104).

Cache hit/miss ratios are logged via `Interlocked` counters (`CartRepository.cs:144–153`).

### 2.3 Feature Flag Caching

`CachedFeatureFlagService` (`src/ECommerce.Infrastructure/Flags/CachedFeatureFlagService.cs`) wraps `IFeatureFlagRepository` with a 30-second Redis TTL:

- On cache hit, deserializes a `FeatureFlagCacheDto` and rehydrates via `FeatureFlag.Rehydrate` (line 45).
- On cache miss, queries EF Core, serializes, and sets with `StringSetAsync` (line 54).
- On Redis failure, gracefully falls back to the repository with a warning log (lines 58–63).

### 2.4 Health Check

`RedisHealthCheck` (`src/ECommerce.Infrastructure/Redis/RedisHealthCheck.cs`) performs a `PING` against the database and is registered at `Infrastructure.DependencyInjection.cs:198`:

```csharp
.AddCheck<RedisHealthCheck>("redis")
```

---

## 3. Configuration

Redis connection is configured via `ConnectionStrings:Redis` in `appsettings.json` (environment-specific overrides):

```json
// appsettings.Development.json
"ConnectionStrings": {
    "Redis": "localhost:6379"
}
```

**Key configuration values:**

| Setting | Location | Default |
|---------|----------|---------|
| `ConnectionStrings:Redis` | `appsettings.*.json` | `localhost:6379` |
| Cart cache TTL | `CartRepository.cs:13` | 30 days |
| Feature flag TTL | `CachedFeatureFlagService.cs:16` | 30 seconds |
| Stampede lock TTL | `CartRepository.cs:12` | 100 ms |
| SignalR channel prefix | `API.DependencyInjection.cs:47` | `signalr:` |

---

## 4. Cache Invalidation Patterns

| Pattern | Implementation | When |
|---------|----------------|------|
| **Write-through** | `CartRepository.SaveAsync` updates cache after DB commit (`CartRepository.cs:136`) | Cart mutation |
| **TTL expiry** | Feature flags expire every 30s, automatically re-fetched | Flag reads |
| **Graceful fallback** | `CachedFeatureFlagService` falls back to DB on Redis failure (`CachedFeatureFlagService.cs:60`) | Redis outage |
| **Lock-based invalidation** | Stampede lock auto-expires after 100ms (`CartRepository.cs:45`) | Concurrent cart reads |

No explicit `KeyDeleteAsync`-based invalidation is used for carts; the long TTL (30 days) acts as implicit expiry for abandoned sessions.

---

## 5. File References

| File | Path |
|------|------|
| Redis health check | `src/ECommerce.Infrastructure/Redis/RedisHealthCheck.cs` |
| Infrastructure DI (connection) | `src/ECommerce.Infrastructure/DependencyInjection.cs` |
| API DI (connection string) | `src/ECommerce.API/DependencyInjection.cs` |
| Cart repository (cache) | `src/ECommerce.Infrastructure/Carts/CartRepository.cs` |
| Cached feature flag service | `src/ECommerce.Infrastructure/Flags/CachedFeatureFlagService.cs` |
| Feature flag repository | `src/ECommerce.Infrastructure/Flags/FeatureFlagRepository.cs` |
| Feature flag domain model | `src/ECommerce.Domain/Flags/FeatureFlag.cs` |
| SignalR Redis backplane | `src/ECommerce.API/DependencyInjection.cs:44–48` |
| Configuration | `src/ECommerce.API/appsettings.Development.json` |
