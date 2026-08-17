# 14 — Checkout & Order Module Design

## Overview

The Checkout & Order module handles the purchase lifecycle: initiating a checkout from a cart, freezing pricing snapshots, authorizing payment, placing orders, and managing the order through fulfillment to delivery. It supports backorders for out-of-stock items on backorderable products, idempotent order placement, and a comprehensive order status timeline with audit logging.

## Domain Entities

### Checkout (`Domain/Orders/Checkout.cs:7`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Checkout` | `CartId`, `CustomerId`, `CustomerEmail`, `Currency`, `PriceSnapshot`, `AppliedCouponId`, `AppliedPromotionIds`, `ShippingAddress`, `BillingAddress`, `ShippingMethodId`, `Status`, `PaymentId`, `ExpiresAt` | Transient; expires if not placed |
| `CheckoutStatus` | `Created`, `PaymentAuthorized`, `Placed`, `Expired` | Four-state lifecycle |

### Order (`Domain/Orders/Order.cs:7`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Order` | `CheckoutId`, `CartId`, `CustomerId`, `OrderNumber`, `CustomerEmail`, `Currency`, `Subtotal`, `ItemDiscount`, `CartDiscount`, `ShippingTotal`, `TaxTotal`, `TaxRate`, `GrandTotal`, `CouponId`, `AppliedPromotionIds`, `ShippingAddress`, `BillingAddress`, `ShippingMethodId`, `PaymentId`, `Status` | Aggregate root |
| `OrderItem` | `OrderId`, `ProductId`, `Sku`, `Name`, `ListPrice`, `UnitPrice`, `Quantity`, `ImageUrl` | Created from `PriceSnapshotItem` |
| `OrderStatusLog` | `OrderId`, `FromStatus`, `ToStatus`, `ActorType`, `ActorId`, `TraceId`, `OccurredAt` | Immutable audit trail |
| `OrderBackorderItem` | `ProductId`, `Sku`, `Quantity`, `FilledQuantity`, `IsFilled` | Backorder tracking |
| `PriceSnapshot` | `Lines`, `Totals` (Subtotal, ItemDiscount, CartDiscount, ShippingTotal, TaxTotal, GrandTotal, TaxRate) | Frozen at checkout time |
| `AddressSnapshot` | Frozen address at order time | Immutable |

### OrderStatus Enum (`Domain/Orders/OrderStatus.cs:3`)

```
Pending → Placed → Backordered → AwaitingFulfillment → Picking → Packed → Shipped → Delivered → Completed
                                   ↓
                               Cancelled
```

## Key Operations

| Operation | Description | Handler |
|---|---|---|
| **Initiate checkout** | Create checkout from cart: verify stock, compute pricing via `PricingEngine`, create payment intent, snapshot prices | `InitiateCheckoutCommand` → `InitiateCheckoutCommandHandler` |
| **Get checkout** | Retrieve checkout with price snapshot | `GetCheckoutQuery` |
| **Place order** | Idempotent: verify checkout+payment status, allocate stock (with backorder handling), capture payment, atomically redeem coupon, create order | `PlaceOrderCommand` → `PlaceOrderCommandHandler` |
| **Get order** | Single order with items and timeline | `GetOrderQuery` |
| **Order history** | Cursor-paginated order list | `GetOrderHistoryQuery` |
| **Cancel order** | Only from `Placed` status; releases stock, records status change | `CancelOrderCommand` |
| **Reorder** | Copy previous order items into a new cart | `ReorderOrderCommand` |
| **Correct shipping address** | Pre-shipment address correction | `CorrectShippingAddressCommand` |

### Order Lifecycle Transitions

- `PlaceOrderCommandHandler` (`Orders/Handlers/PlaceOrderCommandHandler.cs:20`): Orchestrates checkout→order in a single transaction: idempotency check, stock allocation, backorder marking, payment capture, coupon redemption, order creation.
- `StartFulfillment()`: `AwaitingFulfillment → Picking`
- `MarkPacked()`: `Picking → Packed`
- `Ship()`: `Packed → Shipped`
- `Deliver()`: `Shipped → Delivered`
- `Cancel()`: `Placed → Cancelled` (only)
- `MarkBackordered()`: `Placed → Backordered` (when stock shortfalls on backorderable products)
- `FillBackorderItems()`: When stock arrives, fills backorders; transitions to `AwaitingFulfillment` when all filled.

## API Endpoints

### Checkout — `CheckoutController.cs:13` — `/api/v1/checkouts`

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/checkouts` | Initiate checkout (cart ID, addresses, payment method, shipping method) |
| `GET` | `/api/v1/checkouts/{id}` | Get checkout details |
| `POST` | `/api/v1/checkouts/{id}/place` | Place order (requires `Idempotency-Key` header) |

### Orders — `OrdersController.cs:15` — `/api/v1/orders`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/orders` | Cursor-paginated order history (auth required) |
| `GET` | `/api/v1/orders/{orderNumber}` | Order detail with items and status logs |
| `GET` | `/api/v1/orders/{orderNumber}/timeline` | Order status timeline |
| `POST` | `/api/v1/orders/{orderNumber}/cancel` | Cancel order with reason |
| `POST` | `/api/v1/orders/{orderNumber}/reorder` | Create cart from order items |

## Integration Points

- **Cart**: Checkout consumes cart items and coupon code; cart is read at initiation, order is placed against its snapshot.
- **Pricing/Promotions**: `CheckoutTotalsCalculator` runs `PricingEngine.Evaluate()` with active promotions and coupons to produce `PriceSnapshot`.
- **Payments**: `PaymentIntentService` creates the intent at checkout initiation; `PlaceOrderCommandHandler` captures the authorized payment.
- **Inventory**: `IStockAllocator.AllocateAsync()` runs during order placement. Shortfalls on `Backorderable` products trigger `MarkBackordered()`. `BackorderFillService` fills backorders when stock arrives.
- **Fulfillment**: Order status transitions (`Picking`, `Packed`, `Shipped`, `Delivered`) are driven by fulfillment task completion.
- **Tax**: `ITaxCalculator` / `ITaxRateProvider` compute `TaxCalculation` (rate + amount) frozen in the snapshot.
- **Idempotency**: `IIdempotencyKeyRepository` ensures duplicate `PlaceOrder` calls return the same order (keyed on `Idempotency-Key` header).
- **Domain Events**: `CheckoutCreated`, `OrderPlaced`, `OrderCancelled`, `OrderBackordered`, `BackorderFilled`, `OrderStatusChanged`, `OrderShipped`, `OrderDelivered`.

## File References

| File | Purpose |
|---|---|
| `src/ECommerce.Domain/Orders/Checkout.cs` | Checkout aggregate with status lifecycle |
| `src/ECommerce.Domain/Orders/Order.cs` | Order aggregate with backorders and status log |
| `src/ECommerce.Domain/Orders/OrderItem.cs` | Immutable order line from snapshot |
| `src/ECommerce.Domain/Orders/OrderStatus.cs` | 12-state order status enum |
| `src/ECommerce.Domain/Orders/OrderStatusLog.cs` | Immutable status audit record |
| `src/ECommerce.Domain/Orders/OrderBackorderItem.cs` | Backorder tracking entity |
| `src/ECommerce.Domain/Orders/PriceSnapshot.cs` | Frozen pricing at checkout time |
| `src/ECommerce.Domain/Orders/AddressSnapshot.cs` | Frozen address snapshot |
| `src/ECommerce.API/Controllers/CheckoutController.cs` | Checkout REST API |
| `src/ECommerce.API/Controllers/OrdersController.cs` | Orders REST API |
| `src/ECommerce.UseCases/Checkout/Handlers/InitiateCheckoutCommandHandler.cs` | Checkout initiation logic |
| `src/ECommerce.UseCases/Checkout/Services/CheckoutTotalsCalculator.cs` | Price computation |
| `src/ECommerce.UseCases/Checkout/Services/StockAvailabilityVerifier.cs` | Pre-checkout stock check |
| `src/ECommerce.UseCases/Orders/Handlers/PlaceOrderCommandHandler.cs` | Order placement orchestration |
| `src/ECommerce.UseCases/Orders/Handlers/CancelOrderCommandHandler.cs` | Order cancellation with stock release |
| `src/ECommerce.UseCases/Orders/Services/BackorderFillService.cs` | Backorder fulfillment service |
