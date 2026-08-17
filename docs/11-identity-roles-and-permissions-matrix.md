# ECommerce Project — Identity, Roles & Permissions Matrix

## Overview

This document defines the three built-in roles (Admin, Support, Customer), every permission constant defined in `Permissions.cs`, which role holds each permission, and which API endpoints or MediatR commands/queries consume it.

---

## Scope

Covers `src/ECommerce.Shared/Authorization/Permissions.cs` (permission constants), `src/ECommerce.Domain/Identity/Role.cs`, `src/ECommerce.Domain/Identity/RolePermission.cs`, and all controllers in `src/ECommerce.API/Controllers/` that use `[Authorize]` or `IRequirePermission`.

---

## Roles

| Role | Purpose | Default Permission Set |
|------|---------|----------------------|
| **Admin** | Full platform access — can manage all resources, users, roles, integrations, and finance. | All permissions (see below). |
| **Support** | Customer support operations — can look up customers, view and manage orders, and approve refunds. | `CustomersRead`, `CustomersPiiRead`, `OrdersRead`, `OrdersSupportRead`, `ReviewsModerate`, `FinanceInvoiceRead`, `PaymentsRefundApprove`. |
| **Customer** | End-user shopping operations — cart, checkout, order history, profile, reviews, and wishlist. | No platform-level permissions (access is identity-scoped via `[Authorize]` + `ICurrentUser.UserId`). |

---

## Permissions Matrix

### Catalog

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `CatalogProductWrite` | `catalog.product.write` | ✅ | — | — | `ProductsController.Create` (`POST /api/v1/products`) · `ProductsController.Update` (`PATCH /api/v1/products/{id}`) · `CreateProductCommand` · `UpdateProductCommand` |
| `CatalogProductDelete` | `catalog.product.delete` | ✅ | — | — | `ProductsController.Deactivate` (`DELETE /api/v1/products/{id}`) · `DeactivateProductCommand` |

### Inventory — Warehouse

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `InventoryWarehouseRead` | `inventory.warehouse.read` | ✅ | — | — | `WarehousesController.List` (`GET /api/v1/warehouses`) · `WarehousesController.Get` (`GET /api/v1/warehouses/{id}`) · `ListWarehousesQuery` · `GetWarehouseQuery` |
| `InventoryWarehouseWrite` | `inventory.warehouse.write` | ✅ | — | — | `WarehousesController.Create` (`POST /api/v1/warehouses`) · `WarehousesController.Update` (`PATCH /api/v1/warehouses/{id}`) · `CreateWarehouseCommand` · `UpdateWarehouseCommand` |
| `InventoryWarehouseDelete` | `inventory.warehouse.delete` | ✅ | — | — | `WarehousesController.Deactivate` (`DELETE /api/v1/warehouses/{id}`) · `DeactivateWarehouseCommand` |

### Inventory — Stock

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `InventoryStockRead` | `inventory.stock.read` | ✅ | — | — | `StockController.List` (`GET /api/v1/stock`) · `StockController.Get` (`GET /api/v1/stock/{id}`) · `StockController.ListMovements` (`GET /api/v1/stock/movements`) · `ListStockItemsQuery` · `GetStockItemQuery` · `ListStockMovementsQuery` |
| `InventoryStockWrite` | `inventory.stock.write` | ✅ | — | — | `StockController.PostMovement` (`POST /api/v1/stock/movements`) · `StockController.Transfer` (`POST /api/v1/stock/transfers`) · `PostStockMovementCommand` · `TransferStockCommand` |

### Identity & Roles

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `RolesRead` | `roles.read` | ✅ | — | — | `RolesController.List` (`GET /api/v1/roles`) · `ListRolesQuery` |
| `RolesWrite` | `roles.write` | ✅ | — | — | `RolesController.Create` (`POST /api/v1/roles`) · `CreateRoleCommand` |
| `RolesPermissionsWrite` | `roles.permissions.write` | ✅ | — | — | `RolesController.AssignPermissions` (`PUT /api/v1/roles/{id}/permissions`) · `AssignRolePermissionsCommand` |

### Customers

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `CustomersRead` | `customers.read` | ✅ | ✅ | — | `CustomersController.Search` (`GET /api/v1/customers`) · `CustomersController.Get` (`GET /api/v1/customers/{id}`) · `SearchCustomersQuery` · `GetCustomerQuery` |
| `CustomersPiiRead` | `customers.pii.read` | ✅ | ✅ | — | Used in handlers to decide whether PII fields (email, phone) are included in customer responses. |
| `AuthImpersonate` | `auth.impersonate` | ✅ | — | — | `AuthController.Impersonate` (`POST /api/v1/auth/impersonate`) · `ImpersonateUserCommand` |

### Audit

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `AuditRead` | `audit.read` | ✅ | — | — | Audit log query endpoints (admin-only). |

### Feature Flags / Platform

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `PlatformFlagsRead` | `platform.flags.read` | ✅ | — | — | `FeatureFlagsController.List` (`GET /api/v1/flags`) · `FeatureFlagsController.Get` (`GET /api/v1/flags/{key}`) · `ListFeatureFlagsQuery` · `GetFeatureFlagQuery` |
| `PlatformFlagsWrite` | `platform.flags.write` | ✅ | — | — | `FeatureFlagsController.Set` (`PUT /api/v1/flags/{key}`) · `SetFeatureFlagCommand` |

### Orders

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `OrdersRead` | `orders.read` | ✅ | — | — | Used in order query handlers to control data access scope. |
| `OrdersSupportRead` | `orders.support.read` | ✅ | ✅ | — | `OrdersController.Detail` / `Timeline` / `Cancel` — checked inline via `currentUser.Permissions.Contains(Permissions.OrdersSupportRead)` to allow support staff to view any customer's order. `SupportOrdersController.Lookup` (`GET /api/v1/support/orders`). `SupportOrderLookupQuery` |

### Promotions & Coupons

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `PromotionsRead` | `promotions.read` | ✅ | — | — | `PromotionsController.List` (`GET /api/v1/promotions`) · `GetPromotionsQuery` |
| `PromotionsWrite` | `promotions.write` | ✅ | — | — | `PromotionsController.Create` · `Update` · `Activate` · `Pause` · `Schedule` · `CouponsController.Create` · `CreatePromotionCommand` · `UpdatePromotionCommand` · `ActivatePromotionCommand` · `PausePromotionCommand` · `SchedulePromotionCommand` · `CreateCouponCommand` |

### Fulfillment

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `FulfillmentRead` | `fulfillment.read` | ✅ | — | — | `FulfillmentController.ListTasks` · `GetTask` · `GetPickLists` · `ShipmentsController.Get` · `ListFulfillmentQueueQuery` · `GetFulfillmentTaskQuery` · `GetPickListQuery` · `GetShipmentQuery` · `QuoteShippingRateQuery` |
| `FulfillmentWrite` | `fulfillment.write` | ✅ | — | — | `FulfillmentController.CreateTask` · `Assign` · `StartPicking` · `Split` · `Pack` · `CorrectShippingAddress` · `CreateShipment` · `ShipmentsController.ApplyTracking` · All corresponding commands. |

### Invoicing

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `FinanceInvoiceRead` | `finance.invoice.read` | ✅ | ✅ | — | `InvoicesController.List` · `Get` · `DownloadPdf` · `ListCreditNotes` · `ListInvoicesQuery` · `GetInvoiceQuery` · `DownloadInvoicePdfQuery` · `ListCreditNotesQuery` |
| `FinanceInvoiceWrite` | `finance.invoice.write` | ✅ | — | — | Invoice generation and credit note issuance handlers. |

### Payments & Refunds

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `PaymentsRefundApprove` | `payments.refund.approve` | ✅ | ✅ | — | `RefundsController.Approve` (`POST /api/v1/refunds/{id}/approve`) · `ApproveRefundCommand`. Customer can request; support/admin can approve. |
| `FinanceReconcile` | `finance.reconcile` | ✅ | — | — | `ReconciliationController.Run` (`POST /api/v1/reconciliation/run`) · `RunReconciliationCommand` |

### Reviews

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `ReviewsModerate` | `reviews.moderate` | ✅ | ✅ | — | `ReviewsController.ModerationQueue` (`GET /api/v1/reviews/moderate`) · `ReviewsController.Publish` · `Reject` · `Remove` · `GetModerationQueueQuery` · `PublishReviewCommand` · `RejectReviewCommand` · `RemoveReviewCommand` |

### Reporting

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `ReportsRead` | `reports.read` | ✅ | — | — | `ReportsController.Sales` · `Inventory` · `Finance` · `Promotions` · `Fulfillment` · `ExportsController.Start` · `Get` · `Download` · `SalesReportQuery` · `InventoryReportQuery` · `FinanceReportQuery` · `PromotionReportQuery` · `FulfillmentReportQuery` · `CreateExportCommand` · `GetExportQuery` |

### Integrations

| Permission Constant | Permission String | Admin | Support | Customer | Consuming Endpoints / Commands |
|---------------------|-------------------|:-----:|:-------:|:--------:|-------------------------------|
| `IntegrationsRead` | `integrations.read` | ✅ | — | — | `WebhookEndpointsController.List` · `Deliveries` · `ListWebhookEndpointsQuery` · `ListWebhookDeliveriesQuery` |
| `IntegrationsWrite` | `integrations.write` | ✅ | — | — | `WebhookEndpointsController.Create` · `RotateSecret` · `Replay` · `CreateWebhookEndpointCommand` · `RotateWebhookSecretCommand` · `ReplayWebhookCommand` |

---

## Role Summary

| Role | Permissions Count | Permission Strings |
|------|-------------------|--------------------|
| **Admin** | 30 (all) | `catalog.product.write`, `catalog.product.delete`, `inventory.warehouse.read`, `inventory.warehouse.write`, `inventory.warehouse.delete`, `inventory.stock.read`, `inventory.stock.write`, `roles.read`, `roles.write`, `roles.permissions.write`, `customers.read`, `customers.pii.read`, `auth.impersonate`, `audit.read`, `platform.flags.read`, `platform.flags.write`, `orders.read`, `orders.support.read`, `promotions.read`, `promotions.write`, `fulfillment.read`, `fulfillment.write`, `finance.invoice.read`, `finance.invoice.write`, `payments.refund.approve`, `finance.reconcile`, `reviews.moderate`, `reports.read`, `integrations.read`, `integrations.write` |
| **Support** | 7 | `customers.read`, `customers.pii.read`, `orders.support.read`, `finance.invoice.read`, `payments.refund.approve`, `reviews.moderate` |
| **Customer** | 0 | No platform permissions — access is identity-scoped via `[Authorize]` attribute + `ICurrentUser.UserId` on order/cart/profile endpoints. |

---

## How Permissions Are Assigned

1. **At login** (`LoginCommandHandler`), `UserRepository.GetPermissionsAsync` resolves all permission codes via `UserRole` → `Role` → `RolePermission` joins.
2. Permissions are embedded in the JWT `perms` claim by `JwtAccessTokenIssuer`.
3. On each request, `CurrentUser.Permissions` extracts them from the `ClaimsPrincipal`.
4. `AuthorizationBehavior` checks the required permission against this list.

---

## Related Documents

- `docs/10-authentication-and-authorization.md` — Auth flow and token design
- `docs/09-security-architecture.md` — Security architecture
- `docs/08-api-design.md` — Full API surface
- `docs/02-glossary.md` — Permission and role definitions
