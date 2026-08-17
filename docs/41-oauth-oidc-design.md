# OAuth/OIDC Authentication

## Overview

Lightweight OAuth 2.0 implementation built on existing JWT infrastructure. Supports client credentials, resource owner password credentials (ROPC), and a stub authorization code grant.

## Architecture

```
OAuthController ──► ISender (MediatR)
     │
     ├─ ClientCredentialsTokenCommand ──► ClientCredentialsTokenHandler
     │       └─ IOAuthClientValidator + IAccessTokenIssuer
     │
     └─ PasswordTokenCommand ──► PasswordTokenHandler
             └─ IOAuthClientValidator + IUserRepository + IPasswordHasher + IAccessTokenIssuer
```

- **IOAuthClientValidator** — port in UseCases; implemented by `OAuthClientValidatorAdapter` in Infrastructure.
- **OAuthClientStore** — in-memory client registry loaded from `OAuth:Clients` config.
- **OAuthClient** — configuration-defined client with `ClientId`, `ClientSecret`, `AllowedScopes`, `AllowedGrantTypes`.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/auth/oauth/token` | Token endpoint (client_credentials, password) |
| POST | `/api/v1/auth/oauth/revoke` | Stub revocation (returns 204) |
| GET | `/api/v1/auth/oauth/.well-known/openid-configuration` | Discovery document |

## Grant Types

### client_credentials

Machine-to-machine authentication. Client authenticates with `client_id` + `client_secret`. Returns a bearer token scoped to the client's allowed scopes.

### password (ROPC)

Legacy grant for migrating clients. Requires `client_id`, `client_secret`, `username`, `password`. Returns a bearer token with the user's roles and permissions.

### authorization_code (stub)

Returns 501 Not Implemented. Reserved for future OpenIddict integration.

## Configuration

```json
{
  "OAuth": {
    "Issuer": "https://api.example.com",
    "Authority": "https://api.example.com",
    "Clients": [
      {
        "ClientId": "partner-app",
        "ClientSecret": "...",
        "DisplayName": "Partner App",
        "AllowedScopes": ["orders.read", "catalog.read"],
        "AllowedGrantTypes": ["client_credentials"],
        "IsActive": true
      }
    ]
  }
}
```

## Error Codes

| Code | Description |
|------|-------------|
| ERR_OAUTH_001 | Invalid client credentials |
| ERR_OAUTH_002 | Client not authorized for this grant type |
| ERR_OAUTH_003 | Invalid grant parameters |
| ERR_OAUTH_004 | No requested scopes allowed |
| ERR_OAUTH_005 | Invalid username or password |

## Architecture Constraint

`OAuthController` uses only `ISender` (MediatR). It never references `IUserRepository` or any Domain type directly. All Domain type usage is contained within UseCases handlers, preserving the `API → UseCases` dependency direction.
