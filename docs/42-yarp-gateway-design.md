# YARP Reverse Proxy (BFF Gateway)

## Overview

YARP (Yet Another Reverse Proxy) runs in-process within the API project as a Backend-for-Frontend (BFF) gateway layer.

## Configuration

Routes and clusters are defined in `appsettings.json` under `ReverseProxy`:

```json
{
  "ReverseProxy": {
    "Routes": {
      "api-route": {
        "ClusterId": "api-cluster",
        "Match": { "Path": "/api/{**catch-all}" },
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" }
        ]
      },
      "grpc-route": {
        "ClusterId": "api-cluster",
        "Match": { "Path": "/grpc/{**catch-all}", "Grpc": true }
      }
    },
    "Clusters": {
      "api-cluster": {
        "Destinations": {
          "api": {
            "Address": "http://localhost:5000/"
          }
        }
      }
    }
  }
}
```

## Setup

In `Program.cs`:

```csharp
app.MapReverseProxy();
```

In DI:

```csharp
services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));
```

## Health Check

- **Endpoint:** `GET /gateway/health`
- **Response:** `{ "status": "healthy", "timestamp": "..." }`

## Security

- Forwarded headers configured (`X-Forwarded-For`, `X-Forwarded-Proto`)
- Security headers applied before proxy (`CSP`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`)
- HSTS + HTTPS redirection in non-Development environments

## Design Decisions

- **In-process** rather than separate gateway project — reduces deployment complexity; YARP has negligible overhead when hosted alongside the API.
- **Config-driven** routes — transforms, path patterns, and cluster destinations are all in `appsettings.json`, not in code.
- **No rate limiting at gateway** — rate limiting is handled at the application layer via ASP.NET Core middleware.
