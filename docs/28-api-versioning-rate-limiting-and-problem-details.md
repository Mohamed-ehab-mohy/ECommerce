# Document 28 — API Versioning, Rate Limiting & Problem Details

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** API Surface Standards
> **Status:** Draft v1.0
> **Audience:** Engineering, API Consumers

---

## 1. Overview

The API layer enforces three cross-cutting concerns:

1. **URL-based API versioning** with deprecation/sunset headers.
2. **RFC 7807 Problem Details** error responses via `ProblemResponse`.
3. **Login rate limiting** via in-memory throttling.

---

## 2. API Versioning

### 2.1 Strategy

The platform uses **URL path versioning** (`/api/v{N}/...`) governed by `ApiVersionPolicy` (`src/ECommerce.Shared/Api/ApiVersionPolicy.cs`):

| Constant | Value |
|----------|-------|
| `CurrentVersion` | `"1.0"` |
| `CurrentRouteVersion` | `"v1"` |
| `DeprecationSunset` | `"2027-08-31"` |

### 2.2 Middleware

`ApiVersionMiddleware` (`src/ECommerce.API/Common/ApiVersionMiddleware.cs`) runs early in the pipeline (`Program.cs:65`):

1. **Version validation**: Extracts the version segment from the URL via `ApiVersionPolicy.VersionSegment`. If a non-current version is detected (e.g., `/api/v2/...`), returns **404** with a JSON error body (lines 13–22).
2. **Response headers**: For current-version routes, sets `X-API-Version: 1.0` on every response (line 29).
3. **Deprecation headers**: Routes outside `/api/v1/` (e.g., root `/`, unversioned `/health`) receive `Deprecation: true` and `Sunset: 2027-08-31` headers (lines 32–35).

### 2.3 Health Endpoints

Both versioned and unversioned health endpoints are mapped (`Program.cs:67–89`):

| Endpoint | Versioned |
|----------|-----------|
| `/api/v1/health/live` | Yes |
| `/api/v1/health/ready` | Yes |
| `/health/live` | No (deprecated) |
| `/health/ready` | No (deprecated) |

---

## 3. Problem Details (RFC 7807)

### 3.1 Implementation

`ProblemResponse` (`src/ECommerce.API/Common/ProblemResponse.cs`) is a static factory that converts an `OperationError` into a `ProblemDetails` object:

```csharp
public static IActionResult Create(OperationError error)
{
    var problem = new ProblemDetails
    {
        Status = error.StatusCode,
        Type = error.Type,
        Title = Title(error.StatusCode),
        Detail = error.Detail
    };
    problem.Extensions["code"] = error.Code;
    // ... permission, retryAfter, metadata extensions
    return new ObjectResult(problem) { StatusCode = error.StatusCode };
}
```

### 3.2 Response Structure

Every error response follows this shape:

```json
{
    "type": "error-type-uri",
    "title": "Validation Failed",
    "status": 422,
    "detail": "Product name is required.",
    "code": "VALIDATION_FAILED",
    "permission": null,
    "retryAfter": null
}
```

### 3.3 Status Code Mapping

| Status | Title |
|--------|-------|
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 402 | Payment Required |
| 409 | Conflict |
| 422 | Validation Failed |
| 423 | Locked |
| 429 | Too Many Requests |
| 502 | Bad Gateway |

### 3.4 Usage Pattern

Every controller method returns `ProblemResponse.Create(result.ToOperationError())` for error paths. This is the universal error pattern across all 20+ controllers (e.g., `OrdersController.cs:40`, `CartController.cs:178`, `AuthController.cs:169`).

---

## 4. Rate Limiting

### 4.1 Login Throttling

The platform implements **in-memory IP-based login throttling** via `ILoginAttemptThrottler` (registered as singleton at `Infrastructure.DependencyInjection.cs:114`):

```csharp
services.AddSingleton<ILoginAttemptThrottler, InMemoryLoginAttemptThrottler>();
```

Configuration from `appsettings.json`:

| Setting | Key | Default |
|---------|-----|---------|
| Max failed login attempts per account | `Auth:MaxFailedLoginAttempts` | 5 |
| Account lockout duration | `Auth:LockoutDurationMinutes` | 15 |
| Max failed attempts per IP | `Auth:MaxFailedLoginAttemptsPerIp` | 10 |
| IP throttle window | `Auth:LoginAttemptWindowMinutes` | 5 |

When throttled, the `OperationError` carries `RetryAfterSeconds`, which `ProblemResponse` includes as the `retryAfter` extension — enabling clients to implement retry-after logic.

### 4.2 General Rate Limiting

The `System.Threading.RateLimiting` package (8.0.0) is present in the infrastructure dependencies (`packages.lock.json:566`), available for future middleware-level rate limiting as needed.

---

## 5. Configuration

```json
// appsettings.json
"Auth": {
    "RequireVerifiedEmail": true,
    "MaxFailedLoginAttempts": 5,
    "LockoutDurationMinutes": 15,
    "RefreshTokenTtlDays": 30,
    "MaxFailedLoginAttemptsPerIp": 10,
    "LoginAttemptWindowMinutes": 5
}
```

Problem Details is registered via `services.AddProblemDetails()` in `API.DependencyInjection.cs:38`.

---

## 6. File References

| File | Path |
|------|------|
| API version policy | `src/ECommerce.Shared/Api/ApiVersionPolicy.cs` |
| Version middleware | `src/ECommerce.API/Common/ApiVersionMiddleware.cs` |
| Problem response factory | `src/ECommerce.API/Common/ProblemResponse.cs` |
| API DI (ProblemDetails) | `src/ECommerce.API/DependencyInjection.cs:38` |
| Infrastructure DI (throttler) | `src/ECommerce.Infrastructure/DependencyInjection.cs:114` |
| Health endpoints | `src/ECommerce.API/Program.cs:67–89` |
| Configuration | `src/ECommerce.API/appsettings.json:17–24` |
