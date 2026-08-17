# 15 — Pricing, Discount & Promotion Engine

## Overview

The Pricing module implements a multi-tier discount engine with promotions, coupons, stacking policies, and a deterministic pricing pipeline. Promotions define eligibility conditions (product, category, brand, min-quantity, min-amount, segment) and discount actions (item-level, cart-level, shipping-level). A `PricingEngine` evaluates all eligible promotions in priority order, resolves stacking conflicts via each promotion's `StackingMatrix`, and enforces non-negative totals. Coupons link to promotions and add a customer-specific redemption layer.

## Domain Entities

### Promotion (`Domain/Pricing/Promotion.cs:19`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Promotion` | `Name`, `State` (Draft/Active/Paused/Ended), `StartsAt`, `EndsAt`, `Stacking` (StackingMatrix), `Conditions`, `Actions`, `EligibleCountries`, `EligibleCurrencies` | Aggregate root |
| `PromotionState` | `Draft`, `Active`, `Paused`, `Ended` | Lifecycle states |

### Conditions (`Domain/Pricing/PromotionCondition.cs:6`)

Polymorphic JSON-serialized condition hierarchy:

| Condition | Discriminator | Fields |
|---|---|---|
| `ProductCondition` | `"product"` | `ProductIds` |
| `CategoryCondition` | `"category"` | `CategoryIds` |
| `BrandCondition` | `"brand"` | `BrandIds` |
| `MinQuantityCondition` | `"min_qty"` | `MinQuantity` |
| `MinAmountCondition` | `"min_amount"` | `MinAmount` |
| `SegmentCondition` | `"segment"` | `Segment` |

### Discount & Stacking

| Entity | Key Properties | Source |
|---|---|---|
| `DiscountRule` | `Type` (Product/Order/Shipping), `Basis` (Amount/Percent), `Value`, `Cap` | `Domain/Pricing/DiscountRule.cs:19` |
| `StackingMatrix` | `AllowStack`, `AllowStackWith` (list of promotion IDs) | `Domain/Pricing/StackingMatrix.cs:8` |

`DiscountRule.ApplyTo(baseAmount)` computes the discount with cap enforcement, flooring at zero and never exceeding the base amount. Percent values must be ≤ 100.

### Coupon (`Domain/Pricing/Coupon.cs:11`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Coupon` | `Code`, `PromotionId`, `TotalUses`, `UsedCount`, `PerCustomerLimit`, `StartsAt`, `EndsAt` | Links a code to a promotion |
| `CouponUsage` | `CouponId`, `OrderId`, `CustomerId`, `RedeemedAt` | Audit/dedupe row |

Coupon redemption is atomic: the repository performs `UPDATE coupons SET used_count = used_count + 1 WHERE used_count < total_uses` and the domain's `TryRedeem()` enforces the invariant.

### Pricing Pipeline

| Type | Fields | Source |
|---|---|---|
| `PricingLine` | `ProductId`, `Sku`, `ListPrice`, `UnitPrice`, `Quantity`, `CategoryIds`, `BrandIds` | `Domain/Pricing/PricingEngine.cs:3` |
| `PricingContext` | `CustomerId`, `CustomerSegment`, `Country`, `Currency`, `ShippingRate`, `Lines` | `Domain/Pricing/PricingEngine.cs:12` |
| `PricingResult` | `ItemDiscounts`, `CartDiscount`, `ShippingDiscount`, `AppliedRuleIds`, `Subtotal` | `Domain/Pricing/PricingEngine.cs:31` |
| `Money` | `Amount`, `Currency`, `DisplayAmount` | `Domain/Pricing/Money.cs:4` |
| `TaxCalculation` | `Rate` (0..1), `Amount` | `Domain/Pricing/TaxCalculation.cs:7` |

## Key Operations

| Operation | Domain Method | Description |
|---|---|---|
| Create promotion | `Promotion.Create()` | Draft state; validates name, actions, schedule |
| Update promotion | `Promotion.Update()` | Replace conditions/actions/stacking |
| Activate promotion | `Promotion.Activate()` | Draft/Paused → Active; emits `PromotionActivated` |
| Pause promotion | `Promotion.Pause()` | Active → Paused; emits `PromotionPaused` |
| Schedule | `Promotion.Schedule()` | Set start/end window |
| End promotion | `Promotion.End()` | Any → Ended (terminal) |
| Evaluate eligibility | `Promotion.IsEligible(context, utcNow)` | Checks state, schedule, country/currency scope, and all conditions |
| Target lines | `Promotion.TargetLines(context)` | Filters pricing lines by product/category/brand conditions |
| Apply discount | `DiscountRule.ApplyTo(baseAmount)` | Compute discount with cap, floor at 0, cap at base |
| Stack resolution | `PricingEngine.Resolve()` | Additive when all can stack, else best-of |
| Create coupon | `Coupon.Create()` | With total uses, per-customer limit, schedule |
| Redeem coupon | `Coupon.TryRedeem()` + `RecordRedemption()` | Validates schedule and exhaustion; emits `CouponRedeemed` |

### PricingEngine.Evaluate() Pipeline

1. Filter eligible promotions (state=Active, within schedule, country/currency scope, all conditions match)
2. Exclude auto-applied promotions tied to the active coupon (avoids double-count)
3. **Item-level**: For each pricing line, compute best/promo discount; resolve stacking
4. **Cart-level**: Compute order-wide discount on subtotal; stack coupon promotion on top
5. **Shipping-level**: Compute shipping discount
6. Return `PricingResult` with all discounts and applied rule IDs

## API Endpoints

### Promotions — `PromotionsController.cs:13` — `/api/v1/promotions`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/promotions` | List all promotions |
| `POST` | `/api/v1/promotions` | Create promotion (admin) |
| `PATCH` | `/api/v1/promotions/{id}` | Update promotion |
| `POST` | `/api/v1/promotions/{id}/activate` | Activate |
| `POST` | `/api/v1/promotions/{id}/pause` | Pause |
| `POST` | `/api/v1/promotions/{id}/schedule` | Set schedule |

### Coupons — `CouponsController.cs:13` — `/api/v1/coupons`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/coupons` | List all coupons (admin) |
| `POST` | `/api/v1/coupons` | Create coupon (admin) |

## Integration Points

- **Cart**: `ApplyCartCouponCommandHandler` validates the coupon code and stores it on the cart. The coupon's linked promotion is evaluated during checkout.
- **Checkout**: `CheckoutTotalsCalculator` calls `PricingEngine.Evaluate()` with the cart's `PricingContext` and all active promotions to produce `PriceSnapshot`.
- **Order**: `PriceSnapshot` is frozen at checkout time. `Order.AppliedPromotionIds` records which promotions were used.
- **Product Catalog**: `PricingLine.CategoryIds` and `BrandIds` are resolved from the product catalog for promotion targeting.
- **Condition Evaluator**: `PromotionConditionEvaluator.Matches()` (`PricingEngine.cs:38`) handles all six condition types via pattern matching.
- **Domain Events**: `PromotionCreated`, `PromotionActivated`, `PromotionPaused`, `PromotionScheduled`, `CouponCreated`, `CouponRedeemed`.

## File References

| File | Purpose |
|---|---|
| `src/ECommerce.Domain/Pricing/Promotion.cs` | Promotion aggregate with eligibility logic |
| `src/ECommerce.Domain/Pricing/PromotionCondition.cs` | Polymorphic condition hierarchy |
| `src/ECommerce.Domain/Pricing/DiscountRule.cs` | Discount calculation with cap |
| `src/ECommerce.Domain/Pricing/StackingMatrix.cs` | Stacking policy |
| `src/ECommerce.Domain/Pricing/PricingEngine.cs` | Static pricing evaluation service |
| `src/ECommerce.Domain/Pricing/Coupon.cs` | Coupon aggregate |
| `src/ECommerce.Domain/Pricing/CouponUsage.cs` | Redemption audit record |
| `src/ECommerce.Domain/Pricing/Money.cs` | Value type with currency |
| `src/ECommerce.Domain/Pricing/TaxCalculation.cs` | Tax result value object |
| `src/ECommerce.API/Controllers/PromotionsController.cs` | Promotions REST API |
| `src/ECommerce.API/Controllers/CouponsController.cs` | Coupons REST API |
| `src/ECommerce.UseCases/Cart/Handlers/ApplyCartCouponCommandHandler.cs` | Coupon validation at cart level |
| `src/ECommerce.UseCases/Checkout/Services/CheckoutTotalsCalculator.cs` | Pricing engine invocation |
