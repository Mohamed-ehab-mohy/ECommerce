# ECommerce Project — Authentication & Authorization

## Overview

This document describes the authentication and authorization design for the ECommerce platform, covering JWT access tokens, refresh token rotation, email verification, password reset, role-based access control, and the permission-based authorization pipeline.

---

## Scope

Applies to all API endpoints under `src/ECommerce.API/Controllers/` and the identity infrastructure in `src/ECommerce.Infrastructure/Identity/`. Affects every authenticated customer and admin/support user.

---

## Key Concepts

- **JWT Bearer Tokens** — short-lived access tokens signed with RSA-SHA256.
- **Refresh Token Rotation** — long-lived tokens with family-based revocation for breach detection.
- **Email Verification** — required before login; token-based, single-use, time-limited.
- **Password Reset** — token-based flow that also requires re-verification of email.
- **Role-Based Access Control** — named roles with collections of permission strings.
- **Permission-Based Authorization** — fine-grained, per-request permission checks via MediatR pipeline behavior.
- **Account Lockout** — automatic temporary lockout after N failed login attempts.
- **Password Breach Checking** — validation against the HIBP k-Anonymity API.

---

## Implementation Details

### 1. JWT Access Token Issuance

**File:** `src/ECommerce.Infrastructure/Identity/JwtAccessTokenIssuer.cs`

The `JwtAccessTokenIssuer` implements `IAccessTokenIssuer` and produces a signed JWT containing:

| Claim | Source |
|-------|--------|
| `sub` | `Customer.Id` (GUID) |
| `email` | `Customer.Email` |
| `jti` | Unique token ID for revocation |
| `roles` | Role names from `UserRole` → `Role` join |
| `perms` | Permission codes from `Role` → `RolePermission` |

Signing uses an RSA-2048 key managed by `JwtRsaKeyProvider` (`src/ECommerce.Infrastructure/Identity/JwtRsaKeyProvider.cs`), which auto-generates and persists a dev key at `~/.local/ECommerce/jwt-dev.pem` if none is configured. The `JwtOptions` record (`src/ECommerce.Infrastructure/Identity/JwtOptions.cs`) controls issuer, audience, TTL (default 15 minutes), and key source.

### 2. Refresh Token Rotation

**Files:** `src/ECommerce.Domain/Identity/RefreshToken.cs`, `src/ECommerce.Infrastructure/Identity/RefreshTokenRepository.cs`

Each refresh token belongs to a **family** (a `FamilyId` GUID). On every use:

1. The current token is looked up by its SHA-256 hash.
2. If already revoked → the entire family is revoked (replay detection).
3. If valid → it is revoked, a new token is created in the same family, and returned.
4. The `RefreshTokenRepository` supports `RevokeFamilyAsync` for bulk revocation and `RevokeAllByUserAsync` for logout-all.

Key properties:
- `TokenHash` — SHA-256 hash of the opaque token (never stored in plaintext).
- `ExpiresAtUtc` — absolute expiry regardless of rotation.
- `RevokedAtUtc` — set when the token is consumed or revoked.
- `ReplacedById` — links to the token that replaced it.

### 3. Email Verification

**File:** `src/ECommerce.Domain/Identity/Customer.cs`

On registration (`Customer.Register`), a verification token is issued:

```
Customer.IssueVerificationToken(tokenHash, expiresAtUtc)
```

The `VerifyEmail` method validates the token using constant-time comparison (`CryptographicOperations.FixedTimeEquals`), checks expiry, and marks the email as verified. The `CustomerRegistered` domain event carries the plaintext token for notification dispatch.

### 4. Password Reset

**File:** `src/ECommerce.Domain/Identity/Customer.cs`

The flow:
1. `IssuePasswordResetToken` stores the hashed token and raises `PasswordResetRequested`.
2. `ResetPassword` validates the token, sets the new password hash, and **invalidates the email verification** — a new verification token is issued, requiring re-verification.
3. Account lockout state is also cleared on successful reset.

### 5. Account Lockout

**File:** `src/ECommerce.Domain/Identity/Customer.cs`

After `maxAttempts` consecutive failed logins:
- `LockoutEndAtUtc` is set to `utcNow + lockoutDuration`.
- `FailedLoginCount` is reset to 0.
- `IsLockedOut(utcNow)` returns `true` until the lockout expires.
- `RecordSuccessfulLogin` clears both the count and lockout.

### 6. Password Breach Checking

**File:** `src/ECommerce.Infrastructure/Identity/HibpPasswordBreachChecker.cs`

Implements `IPasswordBreachChecker` using the HIBP k-Anonymity API:
1. SHA-1 hashes the password.
2. Sends the first 5 hex characters to `https://api.pwnedpasswords.com/range/`.
3. Checks if the full hash suffix appears in the response.

Called during registration and password reset to reject compromised passwords.

### 7. Password Hashing

**File:** `src/ECommerce.Infrastructure/Identity/BcryptPasswordHasher.cs`

Implements `IPasswordHasher` using BCrypt with work factor 12.

### 8. Role & Permission Storage

**Files:** `src/ECommerce.Domain/Identity/Role.cs`, `RolePermission.cs`, `UserRole.cs`

- `Role` has a `Name`, `Description`, and a collection of `RolePermission` entities.
- `RolePermission` maps a `RoleId` to a `PermissionCode` string.
- `UserRole` maps a `UserId` to a `RoleId`.
- `UserRepository.GetPermissionsAsync` resolves all distinct permission codes for a user via `UserRole` → `Role` → `RolePermission` joins.

### 9. ICurrentUser Abstraction

**Files:** `src/ECommerce.UseCases/Common/ICurrentUser.cs`, `src/ECommerce.API/Common/CurrentUser.cs`

`ICurrentUser` exposes:
- `UserId` — parsed from the JWT `sub` claim.
- `IsAuthenticated` — from `ClaimsPrincipal.Identity`.
- `Roles` — extracted from `roles` claims.
- `Permissions` — extracted from `perms` claims.

Registered as scoped via DI (`src/ECommerce.API.DependencyInjection.cs:42`).

### 10. IRequirePermission + AuthorizationBehavior

**Files:** `src/ECommerce.UseCases/Common/IRequirePermission.cs`, `src/ECommerce.UseCases/Common/AuthorizationBehavior.cs`

Any MediatR request can implement `IRequirePermission` to declare the required permission string:

```csharp
public interface IRequirePermission
{
    string Permission { get; }
}
```

`AuthorizationBehavior<TRequest, TResponse>` is registered as an open MediatR pipeline behavior (`src/ECommerce.UseCases.DependencyInjection.cs:28`). For each request:
1. If the request does not implement `IRequirePermission` → passes through.
2. If the user is authenticated and has the permission → passes through.
3. Otherwise → returns a Forbidden result with an appropriate error.

### 11. Auth Controller Endpoints

**File:** `src/ECommerce.API/Controllers/AuthController.cs`

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `POST /api/v1/auth/register` | Register | None | Creates account, sends verification email |
| `POST /api/v1/auth/verify-email` | VerifyEmail | None | Verifies email token |
| `POST /api/v1/auth/login` | Login | None | Returns access + refresh tokens |
| `POST /api/v1/auth/refresh` | Refresh | None | Rotates refresh token, returns new pair |
| `POST /api/v1/auth/logout` | Logout | Authorized | Revokes the specific refresh token |
| `POST /api/v1/auth/logout-all` | LogoutAll | Authorized | Revokes all refresh tokens for the user |
| `POST /api/v1/auth/forgot-password` | ForgotPassword | None | Issues password reset token |
| `POST /api/v1/auth/reset-password` | ResetPassword | None | Resets password, re-verifies email |
| `POST /api/v1/auth/impersonate` | Impersonate | Authorized + `auth.impersonate` | Admin impersonation |

---

## Related Documents

- `docs/11-identity-roles-and-permissions-matrix.md` — Full permissions matrix
- `docs/09-security-architecture.md` — Security architecture and threat model
- `docs/08-api-design.md` — API endpoint contracts
- `docs/02-glossary.md` — Identity-related term definitions
