# 16 — Inventory & Warehouse Module Design

## Overview

The Inventory module manages stock levels across warehouses using an append-only movement model. Every stock change (receipt, issue, adjustment, allocation, release, fulfillment) is recorded as a `StockMovement` that mutates a `StockItem`'s on-hand and allocated balances. The module supports multi-warehouse management, row-level locking for concurrent-safe allocation, low-stock alerts with cooldown, and inter-warehouse transfers.

## Domain Entities

| Entity | Key Properties | Source |
|---|---|---|
| `StockItem` | `Sku`, `WarehouseId`, `OnHand`, `Allocated`, `Available` (computed: OnHand - Allocated), `Version`, `LowStockThreshold`, `LowStockNotifiedAt`, `LowStockCooldown` | `Domain/Inventory/StockItem.cs:6` |
| `StockMovement` | `StockItemId`, `Type`, `Quantity`, `OnHandDelta`, `AllocatedDelta`, `Reason`, `Reference`, `Note` | `Domain/Inventory/StockMovement.cs:5` |
| `Warehouse` | `Code`, `Name`, `Address`, `Timezone`, `Status` | `Domain/Inventory/Warehouse.cs:5` |

### StockMovementType Enum (`Domain/Inventory/StockMovementType.cs:3`)

| Type | OnHandDelta | AllocatedDelta | Description |
|---|---|---|---|
| `Receipt` | `+quantity` | `0` | Goods received into warehouse |
| `Issue` | `-quantity` | `0` | Manual stock removal |
| `Adjustment` | `+/-quantity` | `0` | Audit correction (allows negative qty) |
| `Allocate` | `0` | `+quantity` | Reserve for an order |
| `Release` | `0` | `-quantity` | Un-reserve (cancellation) |
| `Fulfill` | `-quantity` | `-quantity` | Ship: deduct from both on-hand and allocated |

### WarehouseStatus Enum

`Active`, `Inactive` — warehouses can be deactivated (soft-deleted).

## Key Operations

### StockItem Lifecycle

| Operation | Domain Method | Description |
|---|---|---|
| Create stock item | `StockItem.Create()` | Initialize SKU at a warehouse with threshold |
| Apply movement | `StockItem.Apply(movement)` | Validates: nextOnHand ≥ 0, nextAllocated ≥ 0, nextAvailable ≥ 0. Throws `StockBalanceException` on violation |
| Evaluate low stock | `StockItem.EvaluateLowStock()` | If Available ≤ Threshold and cooldown elapsed, emits `LowStockAlertRaised` |
| Set threshold | `StockItem.SetLowStockThreshold()` | Configure low-stock alert level |
| Set cooldown | `StockItem.SetLowStockCooldown()` | Default 24 hours between alerts |

### Warehouse Operations

| Operation | Domain Method | Description |
|---|---|---|
| Create warehouse | `Warehouse.Create()` | With code, name, address, timezone, status |
| Update warehouse | `Warehouse.UpdateDetails()` | Partial update |
| Deactivate warehouse | `Warehouse.Deactivate()` | Soft-delete |

### StockAllocator (`Infrastructure/Inventory/StockAllocator.cs:8`)

| Operation | Method | Description |
|---|---|---|
| **Allocate** | `AllocateAsync()` | For each SKU, locks stock items `FOR UPDATE`, allocates available across warehouses (FIFO by warehouse code), returns shortfalls |
| **Release** | `ReleaseAsync()` | Reverses allocations (e.g., on order cancellation), decrements `Allocated` |

`AllocateAsync` uses `SELECT ... FOR UPDATE OF si` to serialize concurrent allocations on the same SKU, preventing oversell.

### Inter-Warehouse Transfer

The `TransferStockCommand` creates an `Issue` movement at the source warehouse and a `Receipt` at the destination within a single transaction.

## API Endpoints

### Stock — `StockController.cs:14` — `/api/v1/stock`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/stock` | List stock items (paginated, filterable by warehouse) |
| `GET` | `/api/v1/stock/{stockItemId}` | Get single stock item |
| `GET` | `/api/v1/stock/movements` | List movements for a stock item |
| `POST` | `/api/v1/stock/movements` | Post a stock movement (Receipt/Issue/Adjustment) |
| `POST` | `/api/v1/stock/transfers` | Inter-warehouse transfer |

All endpoints require `[Authorize]`.

### Warehouses — `WarehousesController.cs:14` — `/api/v1/warehouses`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/warehouses` | List warehouses (paginated) |
| `GET` | `/api/v1/warehouses/{id}` | Get warehouse |
| `POST` | `/api/v1/warehouses` | Create warehouse |
| `PATCH` | `/api/v1/warehouses/{id}` | Update warehouse |
| `DELETE` | `/api/v1/warehouses/{id}` | Deactivate warehouse |

## Integration Points

- **Order Placement**: `PlaceOrderCommandHandler` calls `IStockAllocator.AllocateAsync()` during order creation. Shortfalls trigger backorder logic for `Backorderable` products.
- **Order Cancellation**: `CancelOrderCommandHandler` calls `IStockAllocator.ReleaseAsync()` to un-reserve stock.
- **Refund Execution**: `ExecuteRefundCommandHandler` calls `IStockAllocator.ReleaseAsync()` for restock.
- **Fulfillment**: `StockMovementType.Fulfill` is applied when items are shipped (deducts both on-hand and allocated).
- **Backorder Fill**: `BackorderFillService` listens for `StockRestocked` events and fills pending backorders via `Order.FillBackorderItems()`.
- **Low Stock Alerts**: `StockItem.EvaluateLowStock()` emits `LowStockAlertRaised` with cooldown to prevent alert storms.
- **Append-Only Trigger**: Stock movements are never updated or deleted; the append-only pattern ensures a complete audit trail.
- **Domain Events**: `StockRestocked`, `StockTransferred`, `LowStockAlertRaised`.

## File References

| File | Purpose |
|---|---|
| `src/ECommerce.Domain/Inventory/StockItem.cs` | Stock aggregate with movement application |
| `src/ECommerce.Domain/Inventory/StockMovement.cs` | Append-only movement record |
| `src/ECommerce.Domain/Inventory/StockMovementType.cs` | Six movement types |
| `src/ECommerce.Domain/Inventory/Warehouse.cs` | Warehouse entity |
| `src/ECommerce.Domain/Inventory/WarehouseStatus.cs` | Active/Inactive status |
| `src/ECommerce.Domain/Inventory/StockErrors.cs` | Error constants |
| `src/ECommerce.Domain/Exceptions/StockBalanceException.cs` | Balance violation exception |
| `src/ECommerce.Infrastructure/Inventory/StockAllocator.cs` | Row-locking allocator implementation |
| `src/ECommerce.UseCases/Inventory/Ports/IStockAllocator.cs` | Allocator port interface |
| `src/ECommerce.API/Controllers/StockController.cs` | Stock REST API |
| `src/ECommerce.API/Controllers/WarehousesController.cs` | Warehouses REST API |
