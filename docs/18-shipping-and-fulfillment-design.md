# 18 — Shipping & Fulfillment Module Design

## Overview

The Fulfillment module manages warehouse picking, packing, and shipping workflows through a task-based pipeline. `FulfillmentTask` entities progress through Queued → Assigned → Picking → Packed → Shipped, with support for task splitting, zone-based organization, and pick list generation. The Shipment sub-module handles carrier integration, tracking updates with a state machine, and label generation. Both modules are tightly integrated with the Order module for status synchronization.

## Domain Entities

### FulfillmentTask (`Domain/Fulfillment/FulfillmentTask.cs:7`)

| Entity | Key Properties | Notes |
|---|---|---|
| `FulfillmentTask` | `OrderId`, `WarehouseId`, `ParentTaskId`, `Zone`, `Priority`, `Status`, `AssignedTo`, `AssignedAt`, `StartedAt`, `PackedAt`, `ShippedAt`, `CancelledAt`, `CancellationReason`, `Version`, `Items` | Aggregate root |
| `FulfillmentTaskItem` | `TaskId`, `ProductId`, `Sku`, `Quantity`, `BinLocation` | Pick line item |

### FulfillmentTaskStatus Enum (`Domain/Fulfillment/FulfillmentTaskStatus.cs:3`)

```
Queued → Assigned → Picking → Packed → Shipped
   ↓                                     ↑
Cancelled                              │
   (split creates new Queued task) ────┘
```

### Shipment (`Domain/Fulfillment/Shipment.cs:7`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Shipment` | `OrderId`, `FulfillmentTaskId`, `CarrierKey`, `TrackingNumber`, `LabelUrl`, `Status`, `ShippedAt`, `DeliveredAt`, `Updates` | Aggregate root |
| `TrackingUpdate` | `ShipmentId`, `Status`, `OccurredAt`, `Note` | Append-only tracking event |

### ShipmentStatus Enum (`Domain/Fulfillment/ShipmentStatus.cs:3`)

```
Created → InTransit → OutForDelivery → Delivered
              ↓              ↓
           Exception ←───────┘
              ↓
          InTransit (recovery)
```

Valid transitions enforced by `Shipment.CanTransitionTo()` (`Shipment.cs:102`).

## Key Operations

### FulfillmentTask Lifecycle

| Operation | Domain Method | Handler | Description |
|---|---|---|---|
| **Create task** | `FulfillmentTask.Create()` | `CreateFulfillmentTaskCommandHandler` | Creates Queued task for an order/warehouse; emits `FulfillmentTaskCreated` |
| **Add item** | `FulfillmentTask.AddItem()` | — | Add pick line (productId, sku, quantity, binLocation) |
| **Assign** | `FulfillmentTask.Assign(pickerId)` | `AssignFulfillmentTaskCommandHandler` | Queued → Assigned; emits `FulfillmentTaskAssigned` |
| **Start picking** | `FulfillmentTask.StartPicking()` | `StartPickingFulfillmentTaskCommandHandler` | Assigned → Picking; emits `FulfillmentTaskPicking` |
| **Pack** | `FulfillmentTask.MarkPacked()` | `MarkFulfillmentTaskPackedCommandHandler` | Picking → Packed; emits `FulfillmentTaskPacked` |
| **Ship** | `FulfillmentTask.MarkShipped()` | Via `CreateShipmentCommandHandler` | Packed → Shipped; emits `FulfillmentTaskShipped` |
| **Cancel** | `FulfillmentTask.Cancel(reason)` | — | Any except Shipped/Cancelled; emits `FulfillmentTaskCancelled` |
| **Split** | `FulfillmentTask.Split()` | `SplitFulfillmentTaskCommandHandler` | Split items into new child task (only from Queued); emits `FulfillmentTaskSplit` |

### Shipment Lifecycle

| Operation | Handler | Description |
|---|---|---|
| **Create shipment** | `CreateShipmentCommandHandler` | Calls `ICarrierAdapter.CreateShipmentAsync()`, creates `Shipment` aggregate, marks task Shipped, and if all tasks shipped marks Order as Shipped |
| **Apply tracking** | `ApplyShipmentTrackingCommandHandler` | Advances `ShipmentStatus` via `Shipment.ApplyTrackingUpdate()`; validates state machine transitions |
| **Quote shipping rate** | `QuoteShippingRateQueryHandler` | Calls `ICarrierAdapter.QuoteAsync()` via `CarrierRateSelector` |

### Pick List Generation (`UseCases/Fulfillment/Services/PickListGenerationService.cs:6`)

Generates zone-optimized pick lists from assigned/picking tasks:
- Groups lines by warehouse zone
- Sorts within zone by bin location → SKU → task
- Chunks into pages of 25 lines (configurable `maxLinesPerList`)
- Returns `PickListResponse` with zone, warehouse code, and line details

### Task Splitting

`FulfillmentTask.Split()` moves specified items from a Queued task to a new child task linked via `ParentTaskId`. Cannot split if: task is not Queued, moving all items, or no items match. The original task retains remaining items.

### Shipping Address Correction

`CorrectShippingAddressCommand` allows address correction on pre-shipment orders. The `Order.UpdateShippingAddress()` method validates the order is not yet Shipped/Delivered/Completed/Cancelled, then emits `OrderShippingAddressUpdated` and `OrderTimelineUpdated`.

## API Endpoints

### Fulfillment — `FulfillmentController.cs:14` — `/api/v1/fulfillment`

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/fulfillment/tasks` | Create fulfillment task |
| `GET` | `/api/v1/fulfillment/tasks` | List task queue (filter by warehouse, status) |
| `GET` | `/api/v1/fulfillment/tasks/{taskId}` | Get task detail |
| `POST` | `/api/v1/fulfillment/tasks/{taskId}/assign` | Assign picker |
| `POST` | `/api/v1/fulfillment/tasks/{taskId}/start-picking` | Start picking |
| `POST` | `/api/v1/fulfillment/tasks/{taskId}/pack` | Mark packed |
| `POST` | `/api/v1/fulfillment/tasks/{taskId}/split` | Split task |
| `POST` | `/api/v1/fulfillment/shipments` | Create shipment from packed task |
| `GET` | `/api/v1/fulfillment/pick-lists` | Generate pick lists for warehouse |
| `PUT` | `/api/v1/fulfillment/orders/{orderId}/shipping-address` | Correct shipping address |
| `GET` | `/api/v1/fulfillment/shipping-rates/quote` | Quote carrier shipping rate |

All endpoints require `[Authorize]`.

### Shipments — `ShipmentsController.cs:14` — `/api/v1/shipments`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/shipments/{shipmentId}` | Get shipment with tracking updates |
| `POST` | `/api/v1/shipments/{shipmentId}/tracking` | Apply tracking status update |

## Integration Points

- **Order Module**: `CreateShipmentCommandHandler` marks the order as Shipped when all tasks for an order are shipped. Order status progresses: `AwaitingFulfillment → Picking → Packed → Shipped → Delivered`.
- **Inventory**: Fulfillment uses `StockMovementType.Fulfill` to deduct stock (both on-hand and allocated) when items ship.
- **Carrier Integration**: `ICarrierAdapter` interface (`Fulfillment/Shipping/ICarrierAdapter.cs:24`) abstracts carrier APIs (rate quote + shipment creation). `CarrierRateSelector` picks the best rate. `IShippingRateCache` caches quotes.
- **Warehouse Management**: Tasks are scoped to warehouses; pick lists are generated per warehouse zone.
- **Domain Events**: `FulfillmentTaskCreated`, `FulfillmentTaskAssigned`, `FulfillmentTaskPicking`, `FulfillmentTaskPacked`, `FulfillmentTaskShipped`, `FulfillmentTaskSplit`, `FulfillmentTaskCancelled`, `ShipmentCreated`, `ShipmentStatusChanged`, `ShipmentDelivered`.

## File References

| File | Purpose |
|---|---|
| `src/ECommerce.Domain/Fulfillment/FulfillmentTask.cs` | Task aggregate with full lifecycle |
| `src/ECommerce.Domain/Fulfillment/FulfillmentTaskItem.cs` | Pick line entity |
| `src/ECommerce.Domain/Fulfillment/FulfillmentTaskStatus.cs` | 6-state task status |
| `src/ECommerce.Domain/Fulfillment/Shipment.cs` | Shipment aggregate with tracking |
| `src/ECommerce.Domain/Fulfillment/ShipmentStatus.cs` | 5-state shipment status |
| `src/ECommerce.Domain/Fulfillment/TrackingUpdate.cs` | Append-only tracking event |
| `src/ECommerce.Domain/Fulfillment/FulfillmentErrors.cs` | Error constants |
| `src/ECommerce.API/Controllers/FulfillmentController.cs` | Fulfillment REST API |
| `src/ECommerce.API/Controllers/ShipmentsController.cs` | Shipments REST API |
| `src/ECommerce.UseCases/Fulfillment/Handlers/CreateFulfillmentTaskCommandHandler.cs` | Task creation |
| `src/ECommerce.UseCases/Fulfillment/Handlers/FulfillmentTaskStateCommandHandlers.cs` | Assign/Start/Pack state transitions |
| `src/ECommerce.UseCases/Fulfillment/Handlers/SplitFulfillmentTaskCommandHandler.cs` | Task splitting |
| `src/ECommerce.UseCases/Fulfillment/Handlers/CreateShipmentCommandHandler.cs` | Shipment creation + carrier call |
| `src/ECommerce.UseCases/Fulfillment/Handlers/ApplyShipmentTrackingCommandHandler.cs` | Tracking updates |
| `src/ECommerce.UseCases/Fulfillment/Services/PickListGenerationService.cs` | Zone-optimized pick list generation |
| `src/ECommerce.UseCases/Fulfillment/Shipping/ICarrierAdapter.cs` | Carrier abstraction interface |
| `src/ECommerce.UseCases/Fulfillment/Shipping/CarrierRateSelector.cs` | Best-rate selection |
