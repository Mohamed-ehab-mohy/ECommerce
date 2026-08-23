# 03 - Real-time Events (SignalR)

The backend provides real-time updates using **SignalR WebSockets**.

## Hubs
You can connect to the following hubs using the standard `@microsoft/signalr` npm package.

### 1. Orders Hub
**Endpoint:** `http://localhost:5139/hubs/orders`
- **Events Received:** `OrderStatusChanged`, `PaymentSucceeded`, `PaymentFailed`
- **Usage:** Used on the frontend to update the UI when an order's status changes in the background without polling.

### 2. Warehouse Hub
**Endpoint:** `http://localhost:5139/hubs/warehouse`
- **Events Received:** `StockUpdated`, `LowStockAlert`
- **Usage:** Used to show real-time stock availability on the product detail page.

### 3. Admin Hub
**Endpoint:** `http://localhost:5139/hubs/admin`
- **Events Received:** `SystemAlert`, `LiveOpsMetricsUpdated`
- **Usage:** Used by the admin dashboard to show live system metrics and alerts.

## Connecting Example (JS/TS)
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5139/hubs/orders", {
        accessTokenFactory: () => "your-jwt-token" // Optional, if endpoint requires auth
    })
    .withAutomaticReconnect()
    .build();

connection.on("OrderStatusChanged", (orderId, newStatus) => {
    console.log(`Order ${orderId} is now ${newStatus}`);
});

await connection.start();
```
