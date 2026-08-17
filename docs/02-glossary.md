# ECommerce Project — Glossary

## Overview

Domain terms used across the ECommerce platform. Each entry provides a short definition and the primary domain entity (or entities) it maps to.

---

## Identity

| Term | Definition | Entity |
|------|-----------|--------|
| **Customer** | A registered user of the platform with credentials, profile, and locale/currency preferences. | `Customer` (`src/ECommerce.Domain/Identity/Customer.cs`) |
| **Email Verification Token** | A short-lived, single-use token sent on registration; must be verified before login is permitted. | `Customer.VerificationTokenHash` |
| **Password Reset Token** | A short-lived token issued on "forgot password" request; allows setting a new password. | `Customer.PasswordResetTokenHash` |
| **Account Lockout** | Temporary login ban after N consecutive failed attempts; auto-expires after a configured duration. | `Customer.FailedLoginCount`, `Customer.LockoutEndAtUtc` |
| **Refresh Token** | A long-lived opaque token stored as a SHA-256 hash; used to obtain new access tokens without re-authentication. | `RefreshToken` (`src/ECommerce.Domain/Identity/RefreshToken.cs`) |
| **Refresh Token Family** | A group of refresh tokens all derived from the same initial grant; revoking one revokes the entire family (token rotation breach detection). | `RefreshToken.FamilyId` |
| **Device ID** | A client-supplied identifier (`X-Device-Id` header) used to scope refresh tokens per device/session. | `RefreshToken.DeviceId` |
| **Role** | A named collection of permissions assigned to users (e.g., Admin, Support, Customer). | `Role` (`src/ECommerce.Domain/Identity/Role.cs`) |
| **UserRole** | Many-to-many join entity linking a Customer to one or more Roles. | `UserRole` (`src/ECommerce.Domain/Identity/UserRole.cs`) |
| **Permission** | A fine-grained access string (e.g., `catalog.product.write`) assigned to a Role via `RolePermission`. | `RolePermission` (`src/ECommerce.Domain/Identity/RolePermission.cs`) |
| **Impersonation** | An admin-only action that produces a new token pair acting as another user, while recording the impersonator's identity. | `ImpersonateUserCommand`, `AuthController.Impersonate` |
| **Account Closure** | Soft-delete of a customer account; marks `IsDeleted = true` and fires `AccountClosed` domain event. | `Customer.Close()` |
| **Account Erasure (GDPR)** | Anonymises all PII on a closed account (email, name, phone replaced with sentinel values). | `Customer.Anonymize()`, `AccountErased` event |
| **JWT Access Token** | A short-lived RSA-SHA256 signed JWT containing `sub`, `email`, `roles`, and `perms` claims. | `JwtAccessTokenIssuer` (`src/ECommerce.Infrastructure/Identity/JwtAccessTokenIssuer.cs`) |
| **Password Breach Check** | Validates a password against the Have I Been Pwned (HIBP) k-Anonymity API before allowing registration or password reset. | `HibpPasswordBreachChecker` (`src/ECommerce.Infrastructure/Identity/HibpPasswordBreachChecker.cs`) |

---

## Catalog

| Term | Definition | Entity |
|------|-----------|--------|
| **Product** | A sellable item with SKU, slug, pricing, status, and optional translations. | `Product` (`src/ECommerce.Domain/Catalog/Product.cs`) |
| **Product Variant** | A size/color/material variation of a product. | `ProductVariant` (`src/ECommerce.Domain/Catalog/ProductVariant.cs`) |
| **Product Translation** | Locale-specific name/description for a product. | `ProductTranslation` (`src/ECommerce.Domain/Catalog/ProductTranslation.cs`) |
| **Product Status** | Lifecycle state of a product (e.g., Draft, Active, Deactivated). | `ProductStatus` (`src/ECommerce.Domain/Catalog/ProductStatus.cs`) |
| **Product Price** | A monetary amount associated with a product in a specific currency. | `ProductPrice` (`src/ECommerce.Domain/Catalog/ProductPrice.cs`) |
| **Category** | A hierarchical classification node for products. | `Category` (`src/ECommerce.Domain/Catalog/Category.cs`) |
| **Brand** | A manufacturer or brand entity linked to products. | `Brand` (`src/ECommerce.Domain/Catalog/Brand.cs`) |
| **Product Import** | A bulk CSV/Excel import job for creating or updating products asynchronously. | `ProductImport` (`src/ECommerce.Domain/Catalog/ProductImport.cs`) |

---

## Cart

| Term | Definition | Entity |
|------|-----------|--------|
| **Cart** | A per-user (or anonymous via cart key) collection of items intended for checkout. | `Cart` (`src/ECommerce.Domain/Cart/Cart.cs`) |
| **Cart Item** | A single line in the cart — product reference, quantity, and unit price snapshot. | `CartItem` (`src/ECommerce.Domain/Cart/CartItem.cs`) |
| **Cart Key** | An anonymous identifier (`X-Cart-Key` header) used to persist a pre-login cart. | Passed via `AuthController.Login` |
| **Cart Merge** | The process of combining an anonymous cart into an authenticated user's cart on login. | `CartMerged` event |

---

## Checkout

| Term | Definition | Entity |
|------|-----------|--------|
| **Checkout** | An order-in-progress session containing shipping address, delivery method, and line items prior to payment. | `Checkout` (`src/ECommerce.Domain/Orders/Checkout.cs`) |
| **Checkout Status** | Current state of a checkout (e.g., Pending, Completed, Expired). | `CheckoutStatus` (`src/ECommerce.Domain/Orders/CheckoutStatus.cs`) |
| **Idempotency Key** | A client-supplied key ensuring that duplicate order submissions produce a single order. | `IdempotencyKey` (`src/ECommerce.Domain/Orders/IdempotencyKey.cs`) |

---

## Orders

| Term | Definition | Entity |
|------|-----------|--------|
| **Order** | A confirmed purchase record with order number, status, items, shipping address, and payment reference. | `Order` (`src/ECommerce.Domain/Orders/Order.cs`) |
| **Order Number** | A human-readable, sequential order identifier (e.g., `ORD-000001`). | `OrderNumber` (`src/ECommerce.Domain/Orders/OrderNumber.cs`) |
| **Order Item** | A line in an order — product, quantity, and price snapshot at time of purchase. | `OrderItem` (`src/ECommerce.Domain/Orders/OrderItem.cs`) |
| **Order Status** | Lifecycle state of an order (e.g., Placed, Paid, Shipped, Delivered, Cancelled). | `OrderStatus` (`src/ECommerce.Domain/Orders/OrderStatus.cs`) |
| **Order Status Log** | An append-only timeline entry recording every status transition on an order. | `OrderStatusLog` (`src/ECommerce.Domain/Orders/OrderStatusLog.cs`) |
| **Price Snapshot** | A frozen copy of unit price, discount, tax, and currency captured at order placement. | `PriceSnapshot` (`src/ECommerce.Domain/Orders/PriceSnapshot.cs`) |
| **Backorder** | An order item that cannot be fulfilled immediately due to stock shortage; tracked separately with its own status. | `OrderBackorderItem` (`src/ECommerce.Domain/Orders/OrderBackorderItem.cs`) |
| **Address Snapshot** | An immutable copy of the customer's shipping address at order time. | `AddressSnapshot` (`src/ECommerce.Domain/Orders/AddressSnapshot.cs`) |

---

## Pricing

| Term | Definition | Entity |
|------|-----------|--------|
| **Money** | A value object representing a decimal amount in a specific currency. | `Money` (`src/ECommerce.Domain/Pricing/Money.cs`) |
| **Pricing Engine** | The service that evaluates a cart/checkout against active promotions and coupons to compute totals. | `PricingEngine` (`src/ECommerce.Domain/Pricing/PricingEngine.cs`) |
| **Promotion** | A rule-based discount with conditions (eligibility), actions (discount type), and a time window. | `Promotion` (`src/ECommerce.Domain/Pricing/Promotion.cs`) |
| **Promotion Condition** | A predicate evaluated against the cart context (e.g., minimum spend, specific category, country). | `PromotionCondition` (`src/ECommerce.Domain/Pricing/PromotionCondition.cs`) |
| **Discount Rule** | The percentage or fixed-amount discount applied when a promotion's conditions are met. | `DiscountRule` (`src/ECommerce.Domain/Pricing/DiscountRule.cs`) |
| **Stacking Matrix** | A configuration controlling which promotions can be combined on a single order. | `StackingMatrix` (`src/ECommerce.Domain/Pricing/StackingMatrix.cs`) |
| **Coupon** | A single-use or limited-use code tied to a promotion, optionally with per-customer and total-use limits. | `Coupon` (`src/ECommerce.Domain/Pricing/Coupon.cs`) |
| **Coupon Usage** | An immutable record of a coupon being applied by a specific customer on a specific order. | `CouponUsage` (`src/ECommerce.Domain/Pricing/CouponUsage.cs`) |
| **Tax Calculation** | Computed tax amount applied to an order line or order total. | `TaxCalculation` (`src/ECommerce.Domain/Pricing/TaxCalculation.cs`) |

---

## Payments

| Term | Definition | Entity |
|------|-----------|--------|
| **Payment** | A monetary transaction record linked to an order, tracking authorization and capture states. | `Payment` (`src/ECommerce.Domain/Payments/Payment.cs`) |
| **Payment Attempt** | A single try to authorize or capture a payment (may fail or succeed). | `PaymentAttempt` (`src/ECommerce.Domain/Payments/PaymentAttempt.cs`) |
| **Payment Status** | Lifecycle state (e.g., Pending, Authorized, Captured, Failed, Refunded). | `PaymentStatus` (`src/ECommerce.Domain/Payments/PaymentStatus.cs`) |
| **Payment Token** | A tokenised card or payment method reference used for authorization. | `PaymentToken` (`src/ECommerce.Domain/Payments/PaymentToken.cs`) |
| **Payment Ledger Entry** | An immutable journal entry recording every financial movement (debit, credit, refund). | `PaymentLedgerEntry` (`src/ECommerce.Domain/Payments/PaymentLedgerEntry.cs`) |
| **Refund** | A request to return funds for a returned or cancelled order, going through approve → execute flow. | `Refund` (`src/ECommerce.Domain/Payments/Refund.cs`) |
| **Refund Item** | An individual line item within a refund request. | `RefundItem` (`src/ECommerce.Domain/Payments/RefundItem.cs`) |
| **Refund Status** | Lifecycle state (e.g., Requested, Approved, Executing, Completed, Rejected, Failed). | `RefundStatus` (`src/ECommerce.Domain/Payments/RefundStatus.cs`) |
| **Payment Reconciliation Record** | A comparison record between the platform's ledger and the payment provider's statement. | `PaymentReconciliationRecord` (`src/ECommerce.Domain/Payments/PaymentReconciliationRecord.cs`) |

---

## Inventory

| Term | Definition | Entity |
|------|-----------|--------|
| **Warehouse** | A physical or virtual stock location with code, name, address, timezone, and status. | `Warehouse` (`src/ECommerce.Domain/Inventory/Warehouse.cs`) |
| **Warehouse Status** | Lifecycle state (e.g., Active, Inactive). | `WarehouseStatus` (`src/ECommerce.Domain/Inventory/WarehouseStatus.cs`) |
| **Stock Item** | The on-hand quantity of a specific SKU at a specific warehouse. | `StockItem` (`src/ECommerce.Domain/Inventory/StockItem.cs`) |
| **Stock Movement** | An atomic addition or deduction of stock quantity at a warehouse, with reason and reference. | `StockMovement` (`src/ECommerce.Domain/Inventory/StockMovement.cs`) |
| **Stock Movement Type** | The kind of movement (e.g., Receive, Sell, Return, Transfer, Adjust). | `StockMovementType` (`src/ECommerce.Domain/Inventory/StockMovementType.cs`) |
| **Low Stock Alert** | A domain event raised when a stock item falls below its reorder threshold. | `LowStockAlertRaised` event |

---

## Warehouse / Fulfillment

| Term | Definition | Entity |
|------|-----------|--------|
| **Fulfillment Task** | A unit of warehouse work assigned to a picker for an order — includes priority and zone. | `FulfillmentTask` (`src/ECommerce.Domain/Fulfillment/FulfillmentTask.cs`) |
| **Fulfillment Task Item** | An individual order line within a fulfillment task. | `FulfillmentTaskItem` (`src/ECommerce.Domain/Fulfillment/FulfillmentTaskItem.cs`) |
| **Fulfillment Task Status** | Lifecycle state (e.g., Pending, Assigned, Picking, Packed, Shipped). | `FulfillmentTaskStatus` (`src/ECommerce.Domain/Fulfillment/FulfillmentTaskStatus.cs`) |
| **Pick List** | A grouped list of items to pick from a warehouse for a given task or batch. | Queried via `GetPickListQuery` |
| **Shipment** | A dispatched package with carrier, destination, weight, tracking, and status. | `Shipment` (`src/ECommerce.Domain/Fulfillment/Shipment.cs`) |
| **Shipment Status** | Lifecycle state of a shipment (e.g., LabelCreated, InTransit, Delivered). | `ShipmentStatus` (`src/ECommerce.Domain/Fulfillment/ShipmentStatus.cs`) |
| **Tracking Update** | An event from a carrier webhook updating a shipment's transit status. | `TrackingUpdate` (`src/ECommerce.Domain/Fulfillment/TrackingUpdate.cs`) |

---

## Invoicing

| Term | Definition | Entity |
|------|-----------|--------|
| **Invoice** | A financial document issued to a customer for a completed order. | `Invoice` (`src/ECommerce.Domain/Invoicing/Invoice.cs`) |
| **Invoice Number** | A sequential, human-readable invoice identifier. | `InvoiceNumber` (`src/ECommerce.Domain/Invoicing/InvoiceNumber.cs`) |
| **Invoice Line** | A line item on an invoice (product, quantity, amount). | `InvoiceLine` (`src/ECommerce.Domain/Invoicing/InvoiceLine.cs`) |
| **Invoice Status** | Lifecycle state (e.g., Issued, Paid, Credited). | `InvoiceStatus` (`src/ECommerce.Domain/Invoicing/InvoiceStatus.cs`) |
| **Credit Note** | A document issued against an invoice to reverse or adjust a previously invoiced amount. | `CreditNote` (`src/ECommerce.Domain/Invoicing/CreditNote.cs`) |
| **Credit Note Number** | A sequential credit note identifier. | `CreditNoteNumber` (`src/ECommerce.Domain/Invoicing/CreditNoteNumber.cs`) |

---

## Notifications

| Term | Definition | Entity |
|------|-----------|--------|
| **Notification Preference** | Per-customer settings controlling which notification kinds are enabled and on which channels. | `NotificationPreference` (`src/ECommerce.Domain/Notifications/NotificationPreference.cs`) |
| **Notification Channel** | The delivery mechanism (e.g., Email, Push, SMS). | `NotificationChannel` (`src/ECommerce.Domain/Notifications/NotificationChannel.cs`) |
| **Notification Kind** | The category of notification (e.g., OrderConfirmation, PasswordReset, ShipmentUpdate). | `NotificationKind` (`src/ECommerce.Domain/Notifications/NotificationKind.cs`) |

---

## Reviews

| Term | Definition | Entity |
|------|-----------|--------|
| **Product Review** | A customer-submitted rating and comment on a product, subject to moderation before publication. | `ProductReview` (`src/ECommerce.Domain/Reviews/ProductReview.cs`) |
| **Product Review Status** | Lifecycle state (e.g., Pending, Published, Rejected, Removed). | `ProductReviewStatus` (`src/ECommerce.Domain/Reviews/ProductReviewStatus.cs`) |
| **Review Vote** | A helpful/not-helpful vote cast by a customer on a published review. | `ReviewVote` (`src/ECommerce.Domain/Reviews/ReviewVote.cs`) |
| **Review Vote Value** | The vote direction (Helpful or NotHelpful). | `ReviewVoteValue` (`src/ECommerce.Domain/Reviews/ReviewVoteValue.cs`) |

---

## Audit

| Term | Definition | Entity |
|------|-----------|--------|
| **Audit Entry** | A single immutable record of a state change — actor, action, entity, before/after snapshot. | `AuditEntry` (`src/ECommerce.Domain/Audit/AuditEntry.cs`) |
| **Audit Actions** | Enum or constants describing the type of auditable action (e.g., Create, Update, Delete). | `AuditActions` (`src/ECommerce.Domain/Audit/AuditActions.cs`) |
| **Audit Chain** | An append-only, tamper-evident log linking each audit entry to its predecessor via hash. | `AuditChain` (`src/ECommerce.Domain/Audit/AuditChain.cs`) |

---

## Reporting

| Term | Definition | Entity |
|------|-----------|--------|
| **Export Job** | An asynchronous report generation job that produces a CSV file for download. | `ExportJob` (`src/ECommerce.Domain/Reporting/ExportJob.cs`) |
| **Export Report Types** | Enumerated report kinds (e.g., Sales, Inventory, Finance, Promotions, Fulfillment). | `ExportReportTypes` (`src/ECommerce.Domain/Reporting/ExportReportTypes.cs`) |

---

## Feature Flags

| Term | Definition | Entity |
|------|-----------|--------|
| **Feature Flag** | A boolean toggle keyed by string, used to enable or disable features at runtime without deployment. | `FeatureFlag` (`src/ECommerce.Domain/Flags/FeatureFlag.cs`) |

---

## Integrations

| Term | Definition | Entity |
|------|-----------|--------|
| **Webhook Endpoint** | A registered URL that receives POST callbacks for specified domain events, secured with an HMAC signing secret. | Managed via `WebhookEndpointsController` |
| **Webhook Delivery** | A single attempt to deliver a webhook event to an endpoint, with request/response logging. | Managed via `ListWebhookDeliveriesQuery` |
| **Secret Rotation** | The process of generating a new HMAC signing secret for a webhook endpoint and returning it once. | `RotateWebhookSecretCommand` |

---

## Cross-Cutting Concepts

| Term | Definition | Entity |
|------|-----------|--------|
| **Result\<T\>** | A monadic error-handling type used throughout the codebase instead of throwing exceptions. | `Result` (`src/ECommerce.Domain/Common/`) |
| **Domain Event** | An in-process event raised by an aggregate root to trigger side effects (notifications, projections, integrations). | Various under `src/ECommerce.Domain/Events/` |
| **BaseEntity\<TId\>** | Generic abstract base for all domain entities, providing `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, and domain event collection. | `BaseEntity<T>` (`src/ECommerce.Domain/Common/BaseEntity.cs`) |
| **ICurrentUser** | Application-level abstraction for the authenticated user's identity, roles, and permissions. | `ICurrentUser` (`src/ECommerce.UseCases/Common/ICurrentUser.cs`) |
| **IRequirePermission** | Marker interface on MediatR requests that declares the permission string required to execute the command/query. | `IRequirePermission` (`src/ECommerce.UseCases/Common/IRequirePermission.cs`) |
| **AuthorizationBehavior** | A MediatR pipeline behavior that checks `IRequirePermission` against the current user's permissions before the handler runs. | `AuthorizationBehavior` (`src/ECommerce.UseCases/Common/AuthorizationBehavior.cs`) |
| **Operation Error** | A structured error value carried through the Result monad, including code, message, type, and optional retry-after. | `OperationError` |
| **Problem Response** | An RFC 7807 Problem Details JSON response produced from an `OperationError`. | `ProblemResponse` (`src/ECommerce.API/Common/`) |
