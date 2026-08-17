# Document 27 — Real-Time Design (SignalR)

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** Real-Time Communication Architecture
> **Status:** Draft v1.0
> **Audience:** Engineering, Frontend

---

## 1. Overview

The platform uses **ASP.NET Core SignalR** with a **Redis backplane** for horizontally-scalable real-time push to three distinct client audiences:

| Hub | Endpoint | Target Audience | Events |
|-----|----------|-----------------|--------|
| `OrderHub` | `/hubs/orders` | Authenticated customers | `OrderStatusChanged`, `OrderTimelineUpdated` |
| `WarehouseHub` | `/hubs/warehouse` | Operations staff | `NewFulfillmentTask`, `TaskStatusChanged`, `StockAlert` |
| `AdminHub` | `/hubs/admin` | Admins / SuperAdmins | `LiveOrderMetrics`, `StockAlerts`, `ReconciliationDrift` |

All hubs inherit from `RealtimeHubBase` which provides **reconnect replay** via persisted events.

---

## 2. Implementation

### 2.1 Hub Architecture

**Base class** (`src/ECommerce.API/Hubs/RealtimeHubBase.cs`):

- On connection, calls `JoinGroupAndReplayAsync(groupKey)` which:
  1. Adds the connection to the target SignalR group (line 17).
  2. Reads `?lastEventId=<opaque>` from the query string (line 19).
  3. Fetches missed events from `IRealtimeEventStore.GetAfterAsync` (line 25).
  4. Replays up to 200 events to the caller (lines 28–30).

**Concrete hubs:**

- **`OrderHub`** (`src/ECommerce.API/Hubs/OrderHub.cs`): `[Authorize]`. Extracts `sub` claim, joins group `u:{userId}` (line 19).
- **`WarehouseHub`** (`src/ECommerce.API/Hubs/WarehouseHub.cs`): `[Authorize]`. Role-gated to `Staff`, `Admin`, `SuperAdmin` (line 16). Reads `?warehouseId=` query param, joins `wh:{id}` (line 30).
- **`AdminHub`** (`src/ECommerce.API/Hubs/AdminHub.cs`): `[Authorize]`. Role-gated to `Admin`/`SuperAdmin` only (lines 18–19). Aborts connection for unauthorized roles. Joins `admins` group (line 25).

### 2.2 Event Pipeline

The real-time pipeline follows this flow:

```
Domain Event → IEventHandler<T> (broadcaster) → IRealtimeEventForwarder
    → IRealtimeEventStore.AppendAsync (persist to DB)
    → IRealtimeHubContext.SendAsync (push via SignalR)
```

**Broadcasters** (`src/ECommerce.Infrastructure/Realtime/`):

| Broadcaster | Handles Events | Target Hub Context | Group Pattern |
|-------------|---------------|-------------------|---------------|
| `OrderRealtimeBroadcaster.cs` | `OrderStatusChanged`, `OrderTimelineUpdated` | `IOrderRealtimeHubContext` | `u:{customerId}` |
| `WarehouseRealtimeBroadcaster.cs` | `FulfillmentTaskCreated/Assigned/Picking/Packed/Shipped/Cancelled/Split`, `LowStockAlertRaised` | `IWarehouseRealtimeHubContext` | `wh:{warehouseId}` |
| `AdminRealtimeBroadcaster.cs` | `LowStockAlertRaised`, `ReconciliationDriftDetected` | `IAdminRealtimeHubContext` | `admins` |

**Hub contexts** (`src/ECommerce.API/Hubs/RealtimeHubContexts.cs`): Thin wrappers around `IHubContext<THub>` that implement domain-specific interfaces (`IOrderRealtimeHubContext`, etc.), decoupling infrastructure from the API layer.

### 2.3 Event Persistence & Reconnect Replay

`IRealtimeEventStore` (`src/ECommerce.Infrastructure/Realtime/IRealtimeEventStore.cs`) persists every pushed event to a `realtime_events` table (PostgreSQL, `jsonb` data column) with a monotonic `Id` and `GroupKey` index.

On reconnect, the client passes `?lastEventId=<N>` and receives all events with `Id > N` for their group, up to a batch of 200 (`RealtimeHubBase.cs:13`).

### 2.4 Wire Format

All hub messages use the `RealtimeEnvelope` record (`src/ECommerce.Infrastructure/Realtime/RealtimeEnvelope.cs`):

```json
{
    "eventId": 42,
    "type": "OrderStatusChanged",
    "occurredAt": "2026-08-15T10:30:00Z",
    "data": { "orderNumber": "E-20260815-0001", "status": "Shipped" }
}
```

---

## 3. Configuration

### 3.1 Redis Backplane

SignalR uses Redis as a scaleout backplane for multi-instance deployments (`src/ECommerce.API/DependencyInjection.cs:44–48`):

```csharp
services.AddSignalR()
    .AddStackExchangeRedis(configuration.GetConnectionString("Redis")!, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("signalr:");
    });
```

The `signalr:` channel prefix isolates SignalR pub/sub from other Redis usage.

### 3.2 Authentication

All three hubs require JWT Bearer authentication (`[Authorize]` attribute). Role-based access:

- **OrderHub**: Any authenticated user (sees only their own group via `u:{userId}`).
- **WarehouseHub**: `Staff`, `Admin`, `SuperAdmin` roles.
- **AdminHub**: `Admin`, `SuperAdmin` roles only.

### 3.3 Endpoint Mapping

Hubs are mapped in `Program.cs:129–131`:

```csharp
app.MapHub<OrderHub>("/hubs/orders");
app.MapHub<WarehouseHub>("/hubs/warehouse");
app.MapHub<AdminHub>("/hubs/admin");
```

---

## 4. File References

| File | Path |
|------|------|
| Hub base class | `src/ECommerce.API/Hubs/RealtimeHubBase.cs` |
| Order hub | `src/ECommerce.API/Hubs/OrderHub.cs` |
| Warehouse hub | `src/ECommerce.API/Hubs/WarehouseHub.cs` |
| Admin hub | `src/ECommerce.API/Hubs/AdminHub.cs` |
| Hub context adapters | `src/ECommerce.API/Hubs/RealtimeHubContexts.cs` |
| Event types & envelope | `src/ECommerce.Infrastructure/Realtime/RealtimeEnvelope.cs` |
| Event store interface | `src/ECommerce.Infrastructure/Realtime/IRealtimeEventStore.cs` |
| Event store (EF Core) | `src/ECommerce.Infrastructure/Realtime/RealtimeEventStore.cs` |
| Event forwarder | `src/ECommerce.Infrastructure/Realtime/RealtimeEventForwarder.cs` |
| Event table config | `src/ECommerce.Infrastructure/Realtime/RealtimeEventConfiguration.cs` |
| Order broadcaster | `src/ECommerce.Infrastructure/Realtime/OrderRealtimeBroadcaster.cs` |
| Warehouse broadcaster | `src/ECommerce.Infrastructure/Realtime/WarehouseRealtimeBroadcaster.cs` |
| Admin broadcaster | `src/ECommerce.Infrastructure/Realtime/AdminRealtimeBroadcaster.cs` |
| Hub registration | `src/ECommerce.API/Program.cs:129–131` |
| Redis backplane config | `src/ECommerce.API/DependencyInjection.cs:44–48` |
