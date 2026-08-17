# Document 29 — Feature Flags & Configuration Management

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** Feature Flag System & Configuration Pattern
> **Status:** Draft v1.0
> **Audience:** Engineering, Product, DevOps

---

## 1. Overview

The platform implements a **database-backed feature flag system** with Redis caching for gradual rollouts and kill-switches. Configuration management follows the standard ASP.NET Core layered pattern (`appsettings.json` → environment overrides → environment variables).

---

## 2. Feature Flag System

### 2.1 Architecture

```
IFeatureFlagService.IsEnabledAsync(key)
    ↓
CachedFeatureFlagService (Redis, 30s TTL)
    ↓ (cache miss)
IFeatureFlagRepository (EF Core → PostgreSQL)
    ↓
FeatureFlag domain entity
```

Three layers ensure performance with consistency:

| Layer | Component | Purpose |
|-------|-----------|---------|
| Interface | `IFeatureFlagService` | Evaluates flags; any context may call |
| Cache | `CachedFeatureFlagService` | 30-second Redis cache with graceful fallback |
| Storage | `FeatureFlagRepository` | EF Core query against `feature_flags` table |

### 2.2 Domain Model

`FeatureFlag` (`src/ECommerce.Domain/Flags/FeatureFlag.cs`) extends `BaseEntity<Guid>`:

| Property | Type | Purpose |
|----------|------|---------|
| `Key` | `string` | Unique lookup key (e.g., `"new-checkout-flow"`) |
| `Description` | `string` | Human-readable explanation |
| `Enabled` | `bool` | On/off toggle |

Factory methods: `Create(key, description, enabled, utcNow)` for new flags, `Rehydrate(key, description, enabled)` for cache reconstruction.

### 2.3 Cache Implementation

`CachedFeatureFlagService` (`src/ECommerce.Infrastructure/Flags/CachedFeatureFlagService.cs`):

- **Cache key**: `feature-flag:{key}` (line 14)
- **TTL**: 30 seconds (line 16)
- **Serialization**: `FeatureFlagCacheDto` record (line 67) stored as JSON
- **Cache miss**: Queries `IFeatureFlagRepository.GetByKeyAsync`, serializes and stores (lines 49–54)
- **Graceful degradation**: On Redis exception, logs warning and falls back to repository (lines 58–63):

```csharp
catch (Exception exception)
{
    logger.LogWarning(exception, "Feature flag cache access failed for key {Key}; falling back to repository.", key);
    return await repository.GetByKeyAsync(key, cancellationToken);
}
```

### 2.4 Repository

`FeatureFlagRepository` (`src/ECommerce.Infrastructure/Flags/FeatureFlagRepository.cs`) queries the `feature_flags` table via EF Core with `GetByKeyAsync` and `ListAsync`.

### 2.5 Usage Pattern

Any use case handler or service can evaluate a flag:

```csharp
var isEnabled = await featureFlagService.IsEnabledAsync("new-checkout-flow", cancellationToken);
if (isEnabled)
{
    // new behavior
}
```

The bounded context spec (`docs/06c-bounded-contexts.md:§4.14`) defines Feature Flags as a **Generic** context:

> *Any context may evaluate flags via `IFeatureFlagEvaluator`. Flag changes audited; evaluation cached 30 s; kill-switch semantics honored.*

### 2.6 DI Registration

```csharp
// Infrastructure.DependencyInjection.cs:117–118
services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
services.AddScoped<IFeatureFlagService, CachedFeatureFlagService>();
```

---

## 3. Configuration Management

### 3.1 Layered Configuration

The platform follows ASP.NET Core's standard configuration hierarchy:

| Layer | Source | Purpose |
|-------|--------|---------|
| Base | `appsettings.json` | Defaults for all environments |
| Environment | `appsettings.{Environment}.json` | Dev/staging/prod overrides |
| Environment variables | `ConnectionStrings__Redis`, etc. | Container/secrets injection |

### 3.2 Key Configuration Sections

| Section | Purpose | Example Keys |
|---------|---------|-------------|
| `ConnectionStrings` | Database connections | `Postgres`, `Redis`, `RabbitMq` |
| `Jwt` | JWT token settings | `Issuer`, `Audience`, `AccessTokenTtlMinutes` |
| `Auth` | Auth policies | `MaxFailedLoginAttempts`, `LockoutDurationMinutes` |
| `Outbox` | Outbox polling | `PollingIntervalSeconds`, `BatchSize` |
| `Logging` | Log levels | `Default`, `Microsoft.AspNetCore` |

### 3.3 Environment-Specific Overrides

`appsettings.Development.json` overrides connection strings for local development:

```json
"ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5433;Database=ecommerce;Username=ecommerce;Password=ecommerce_dev_pw",
    "Redis": "localhost:6379",
    "RabbitMq": "amqp://guest:guest@localhost:5672"
}
```

### 3.4 Strongly-Typed Options

Several configuration sections are bound to strongly-typed options classes via `IOptions<T>`:

| Options Class | Configuration Section | Registration |
|---------------|----------------------|-------------|
| `JwtOptions` | `Jwt` | `API.DependencyInjection.cs:60` |
| `AuthSettings` | `Auth` | `API.DependencyInjection.cs:61` |
| `SmtpOptions` | `Smtp` | `API.DependencyInjection.cs:35` |
| `PaymentProviderOptions` | `PaymentProvider` | `Infrastructure.DependencyInjection.cs:214` |
| `WebhookOptions` | `Webhook` | `Infrastructure.DependencyInjection.cs:187` |

### 3.5 Environment Variables

Sensitive values (database passwords, API keys) are intended to be injected via environment variables in Docker/Kubernetes:

```bash
ConnectionStrings__Redis=redis:6379
ConnectionStrings__Postgres=Host=db;Port=5432;Database=ecommerce;Username=...
Jwt__Issuer=ecommerce-api
```

---

## 4. File References

| File | Path |
|------|------|
| Feature flag domain model | `src/ECommerce.Domain/Flags/FeatureFlag.cs` |
| Feature flag service interface | `src/ECommerce.UseCases/Flags/Ports/IFeatureFlagService.cs` |
| Cached feature flag service | `src/ECommerce.Infrastructure/Flags/CachedFeatureFlagService.cs` |
| Feature flag repository | `src/ECommerce.Infrastructure/Flags/FeatureFlagRepository.cs` |
| Infrastructure DI (registration) | `src/ECommerce.Infrastructure/DependencyInjection.cs:117–118` |
| Configuration | `src/ECommerce.API/appsettings.json` |
| Dev configuration | `src/ECommerce.API/appsettings.Development.json` |
| Bounded context spec (§4.14) | `docs/06c-bounded-contexts.md:279–286` |
