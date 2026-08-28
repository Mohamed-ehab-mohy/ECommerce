# 02 - API Reference (Request & Response Shapes)

This document describes the endpoints and the exact **response shapes** the frontend can expect. All JSON uses **camelCase** property names. Base URL: `http://localhost:5139`.

- **Explore & test live:** [Swagger UI](http://localhost:5139/swagger)
- **GraphQL IDE (catalog):** [http://localhost:5139/graphql](http://localhost:5139/graphql)

---

## Authentication

Most endpoints require a JWT bearer token:

```http
Authorization: Bearer <your-jwt-token>
```

Get one via `POST /api/v1/auth/login` (see below).

---

## Error Shape (unified)

All errors (except a few noted exceptions) use **RFC 7807 Problem Details** with a `code` in the `extensions`:

```json
{
  "status": 409,
  "type": "problems/conflict",
  "title": "Conflict",
  "detail": "...",
  "code": "ERR_RES_001",
  "permission": "customers.read",
  "retryAfter": 30
}
```

- `permission` appears only on authorization/forbidden errors.
- `retryAfter` (seconds) appears only when relevant (e.g. login throttling).
- HTTP→`type` mapping: `422`→`problems/validation-failed`, `409`→`problems/conflict`, `404`→`problems/not-found`, `401`→`problems/unauthorized`, `403`→`problems/forbidden`, `423`→`problems/locked`, `429`→`problems/rate-limited`, `400`→`problems/bad-request`, `502`→`problems/upstream-unavailable`.

**Validation errors** (422) add an `errors` field:

```json
{
  "type": "problems/validation-failed",
  "title": "Validation failed",
  "status": 422,
  "detail": "One or more validation failures have occurred.",
  "errors": { "email": ["'Email' is not a valid email address."] }
}
```

---

## Auth — `/api/v1/auth`

| Method | Path | Success |
|---|---|---|
| POST | `/auth/register` | 201 |
| POST | `/auth/verify-email` | 200 |
| POST | `/auth/login` | 200 |
| POST | `/auth/refresh` | 200 |
| POST | `/auth/logout` | 204 |
| POST | `/auth/logout-all` | 204 |
| POST | `/auth/forgot-password` | 202 |
| POST | `/auth/reset-password` | 200 |
| POST | `/auth/impersonate` | 200 |

**POST /auth/register → 201**

```json
{ "userId": "6f0c2b19-...", "status": "pendingVerification", "message": "Verification email sent." }
```

**POST /auth/login → 200** (also used by `/refresh`)

```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "string",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": { "id": "guid", "email": "a@b.com", "roles": ["Customer"] }
}
```

- Optional headers: `X-Device-Id`, `X-Cart-Key`.
- Errors: `AuthErrors.TooManyAttempts` → **429** (`Retry-After` header set), `AuthErrors.AccountLocked` → **423** with `retryAfter`.

**POST /auth/forgot-password → 202**

```json
{ "status": "accepted" }
```

**POST /auth/reset-password → 200**

```json
{ "status": "passwordReset" }
```

**POST /auth/impersonate → 200** (`[Authorize]`)

```json
{
  "accessToken": "string", "refreshToken": "string", "expiresIn": 3600, "tokenType": "Bearer",
  "impersonatorId": "guid",
  "user": { "id": "guid", "email": "a@b.com", "roles": ["..."] }
}
```

**OAuth (partner tokens)** — `POST /api/v1/auth/oauth/token` (form-urlencoded `grant_type`, `client_id`, `client_secret`):

```json
{ "access_token": "string", "token_type": "Bearer", "expires_in": 3600, "scope": "string" }
```

- This controller uses OAuth error format (`error` / `error_description`), **not** ProblemDetails.

---

## Profile / Customers

### Profile — `/api/v1/me` (`[Authorize]`)

- `GET /me` → 200 `ProfileResponse`

```json
{
  "id": "guid", "email": "a@b.com", "displayName": "John",
  "phone": "string|null", "locale": "en", "currency": "USD", "emailVerified": false
}
```

- `PATCH /me` → 200 (updated `ProfileResponse`)
- `GET /me/addresses` → 200 `AddressResponse[]`:

```json
[
  { "id": "guid", "label": "Home|null", "street": "10 Main", "city": "Cairo", "region": "Giza|null", "country": "EG", "postalCode": "12345|null", "createdAt": "2026-08-28T10:00:00Z" }
]
```

- `POST /me/addresses` → 201 `{ "id": "guid" }`
- `DELETE /me/addresses/{addressId}` → 204
- `POST /me/close` → 204 · `POST /me/erase` → 204
- `GET /me/export` → 200 `PersonalDataExport` (email, locale, registeredAt, orders[], addresses[], roles[])

### Customers — `/api/v1/customers` (`[Authorize]`, needs `customers.read`)

- `GET /customers?email=&page=&pageSize=` → 200 `PagedCustomersResponse`

```json
{
  "items": [
    { "id": "guid", "email": "j***@example.com", "displayName": "John", "phone": "string|null", "locale": "en", "currency": "USD", "emailVerified": false, "createdAt": "2026-08-28T10:00:00Z" }
  ],
  "page": 1, "pageSize": 20, "totalCount": 150
}
```

> **PII masking:** email/phone are masked unless the caller has the `CustomersPiiRead` permission.

- `GET /customers/{customerId}` → 200 single item. `CustomerErrors.CustomerNotFound` → 404.

### Roles — `/api/v1/roles` (`[Authorize]`)

- `GET /roles` → 200 `RoleResponse[]` `[{ "id": "guid", "name": "Admin", "description": "string", "permissions": ["customers.read"] }]`
- `POST /roles` → 201 `{ "id": "guid" }`
- `PUT /roles/{roleId}/permissions` → 204

### MFA — `/api/v1/mfa` (`[Authorize]`)

- `POST /mfa/setup` → 200 `{ "secretKey": "string", "totpUri": "string", "qrCodeUrl": "string" }`
- `POST /mfa/verify` → 200 `{ "verified": true }`
- Errors: `Mfa.NotSetup` (404), `Mfa.Locked` (423), `Mfa.InvalidCode` (401)

---

## Catalog

### Products — `/api/v1/products` (public reads)

**GET /products** — with any search filter (`q`, `categoryId`, `brandId`, `price.gte`, `price.lte`, `rating.gte`) returns `SearchProductsResponse`; otherwise `PagedProductsResponse`.

Paged (plain list):

```json
{
  "items": [
    { "id": "guid", "sku": "SKU-001", "slug": "name", "name": "Product", "description": "string|null", "currency": "USD", "listAmount": 100.00, "offerAmount": 80.00, "status": 1, "isFeatured": false, "categoryId": "guid|null", "brandId": "guid|null" }
  ],
  "page": 1, "pageSize": 20, "totalCount": 123
}
```

Search (adds facets):

```json
{
  "items": [ "same as above" ],
  "facets": {
    "categories": [ { "id": "guid", "name": "string", "count": 3 } ],
    "brands": [ { "id": "guid", "name": "string", "count": 2 } ],
    "priceRanges": [ { "key": "string", "label": "string", "min": 0.0, "max": 100.0, "count": 5 } ],
    "ratings": [ { "stars": 5, "count": 10 } ]
  },
  "page": 1, "pageSize": 20, "totalCount": 123, "hasNext": true
}
```

> `status` is a numeric enum: `0`=Draft, `1`=Active, `2`=Inactive.

- `GET /products/{productId}` → 200 single `ProductResponse`
- `POST /products` → 201 `{ "id": "guid" }` (`[Authorize]`)
- `PATCH /products/{productId}` → 204 · `DELETE /products/{productId}` → 204
- `GET /products/autocomplete?q=&limit=10` → 200 `[{ "productId": "guid", "name": "string", "sku": "string", "listAmount": 100.0 }]`

### Categories — `/api/v1/categories`

`GET /categories` → 200 tree array:

```json
[ { "id": "guid", "name": "string", "slug": "string", "parentId": "guid|null", "sortOrder": 1, "level": 0, "children": [ "recursive" ] } ]
```

`POST /categories` → 201 `{ "id": "guid" }` · `PATCH /categories/{id}` → 204

### Brands — `/api/v1/brands`

`GET /brands?page=&pageSize=` → 200 `{ "items": [{ "id": "guid", "name": "string", "description": "string|null", "website": "string|null" }], "page": 1, "pageSize": 20, "totalCount": 5 }`
`POST /brands` → 201 `{ "id": "guid" }` · `PATCH /brands/{id}` → 204

### Reviews — `/api/v1`

- `GET /products/{productId}/reviews?page=&pageSize=` → 200 `ProductReviewsResponse`:

```json
{
  "productId": "guid", "ratingAverage": 4.5, "ratingCount": 12, "page": 1, "pageSize": 20, "total": 5,
  "items": [ { "reviewId": "guid", "productId": "guid", "rating": 5, "comment": "string", "verifiedPurchase": true, "publishedAtUtc": "datetime|null", "helpfulVotes": 3 } ]
}
```

- `POST /products/{productId}/reviews` → 202 `{ "reviewId": "guid", "productId": "guid", "status": "string", "verifiedPurchase": true }` (`[Authorize]`)
- `POST /reviews/{reviewId}/vote` → 200 `{ "reviewId": "guid", "helpfulVotes": 4 }`
- `GET /reviews/moderate` → 200 `ModerationQueueResponse` · `POST /reviews/{id}/publish` / `reject` / `remove` → 200 `{ "reviewId": "guid", "status": "string", "moderatorId": "guid|null", "moderatedAtUtc": "datetime|null" }`

### Other catalog

- `GET /api/v1/currencies` → 200 `["USD","EUR","GBP"]`
- `GET /api/v1/currencies/rates?baseCurrency=USD` → 200 `{ "baseCurrency": "USD", "rates": [{ "fromCurrency": "USD", "toCurrency": "EUR", "rate": 0.85, "updatedAt": "datetime" }] }`
- `GET /api/v1/currencies/convert?amount=100&from=USD&to=EUR` → 200 `{ "originalAmount": 100.0, "fromCurrency": "USD", "toCurrency": "EUR", "convertedAmount": 85.0 }`
- `GET /api/v1/recommendations/for-me?limit=` (`[Authorize]`) → 200 `[{ "productId": "guid", "sku": "string", "name": "string", "price": 10.0, "score": 0.9, "reason": "string" }]` (also `/bought-together/{productId}` and `/trending`)
- `POST /api/v1/content/banners` → 201 `BannerResponse`
- `GET /api/v1/flags` → 200 `[{ "key": "string", "description": "string", "enabled": true }]` · `GET /api/v1/flags/{key}` → 200 · `PUT /api/v1/flags/{key}` → 204

### Product imports — `/api/v1/imports` (`[Authorize]`)

- `POST /imports/products` → 202 `{ "importId": "guid", "status": "string" }` (+ `Location` header)
- `GET /imports/{importId}` → 200 `ProductImportStatusResponse` (status, totalRows, succeededRows, failedRows, errors[])

---

## Cart — `/api/v1/carts/me`

Uses `X-Cart-Key` header for anonymous carts; returns a refreshed `X-Cart-Key` when needed.

`CartResponse` (most endpoints):

```json
{
  "id": "guid",
  "currency": "USD",
  "version": 1,
  "expiresAt": "2026-08-30T10:00:00Z",
  "updatedAt": "2026-08-28T10:00:00Z",
  "appliedCouponCode": "SAVE10",
  "items": [
    { "productId": "guid", "sku": "SKU-001", "name": "Product", "imageUrl": "https://...", "listPrice": 100.0, "unitPrice": 80.0, "quantity": 2, "lineSubtotal": 160.0, "lineDiscount": 40.0 }
  ],
  "totals": { "subtotal": 200.0, "itemDiscount": 40.0, "shipping": 9.9, "tax": 8.0, "total": 177.9 }
}
```

`GET /carts/me/price-changes` → 200 `{ "warnings": [{ "productId": "guid", "sku": "string", "name": "string", "cartUnitPrice": 10.0, "currentUnitPrice": 12.0, "delta": 2.0 }] }`

| Method | Path | Success |
|---|---|---|
| GET | `/carts/me` | 200 `CartResponse` |
| POST | `/carts/me/items` | 200 `CartResponse` |
| PATCH | `/carts/me/items/{productId}` | 200 `CartResponse` |
| DELETE | `/carts/me/items/{productId}` | 200 `CartResponse` |
| GET | `/carts/me/price-changes` | 200 |
| POST | `/carts/me/coupons` | 200 `CartResponse` |
| DELETE | `/carts/me/coupons` | 200 `CartResponse` |

Cart error codes: `Cart.QuantityOutOfRange` (422), `Cart.ItemNotFound`/`Cart.CartNotFound` (404), `Cart.ProductInactive` (409), `Cart.ConcurrencyConflict` (409), `Cart.InvalidPrice`/`Cart.UnsupportedCurrency` (422).

---

## Wishlist — `/api/v1/wishlist` (`[Authorize]`)

`WishlistResponse`:

```json
{ "id": "guid", "updatedAt": "2026-08-28T10:00:00Z", "items": [ { "productId": "guid", "addedAt": "2026-08-28T09:00:00Z" } ] }
```

| Method | Path | Success |
|---|---|---|
| GET | `/wishlist` | 200 `WishlistResponse` |
| POST | `/wishlist/items` | 200 `WishlistResponse` |
| DELETE | `/wishlist/items/{productId}` | 200 `WishlistResponse` |
| POST | `/wishlist/items/{productId}/move?currency=` | 200 `CartResponse` |

---

## Coupons & Promotions

### Coupons — `/api/v1/coupons` (`[Authorize]`)

`CouponResponse`:

```json
{ "id": "guid", "code": "SAVE10", "promotionId": "guid", "totalUses": 100, "usedCount": 3, "perCustomerLimit": 1, "startsAt": "datetime|null", "endsAt": "datetime|null", "createdAt": "datetime", "updatedAt": "datetime|null" }
```

- `GET /coupons` → 200 `CouponResponse[]` · `POST /coupons` → 201 `CouponResponse`
- Codes: `ERR_CPN_001..004` (422), `ERR_RES_001` (404)

### Promotions — `/api/v1/promotions` (`[Authorize]`)

`PromotionResponse`:

```json
{
  "id": "guid", "name": "Summer Sale", "state": "Active",
  "startsAt": "datetime|null", "endsAt": "datetime|null",
  "allowStack": true, "allowStackWith": ["guid"],
  "conditions": [ { "type": "product", "productIds": ["guid"] }, { "type": "min_amount", "minAmount": 50.0 } ],
  "actions": [ { "type": "Order", "basis": "Percent", "value": 10.0, "cap": 20.0 } ],
  "eligibleCountries": ["US"], "eligibleCurrencies": ["USD"],
  "createdAt": "datetime", "updatedAt": "datetime|null"
}
```

- `GET /promotions` → 200 `[]` · `POST` → 201 · `PATCH /promotions/{id}` → 200 · `/promotions/{id}/activate|pause|schedule` → 200 (all `PromotionResponse`)
- Codes: `ERR_PROMO_001..006` (422/409), `ERR_RES_001` (404)

---

## Checkout — `/api/v1/checkouts`

`CheckoutResponse` (Initiate / Get):

```json
{
  "checkoutId": "guid", "cartId": "guid", "currency": "USD", "status": "Created",
  "lines": [ { "productId": "guid", "sku": "SKU-1", "name": "X", "listPrice": 100.0, "unitPrice": 80.0, "quantity": 2, "imageUrl": "https://..." } ],
  "totals": { "subtotal": 200.0, "itemDiscount": 40.0, "cartDiscount": 10.0, "shippingTotal": 9.9, "taxTotal": 8.0, "grandTotal": 167.9, "currency": "USD" },
  "payment": { "clientToken": "tok_...", "providerKey": "stripe", "paymentId": "guid" },
  "expiresAt": "2026-08-28T11:00:00Z"
}
```

`status` values: `Created`, `PaymentAuthorized`, `Placed`, `Expired`.

- `POST /checkouts` → **201** + `Location` header · `GET /checkouts/{id}` → 200
- `POST /checkouts/{checkoutId}/place` → 200 `OrderResponse` (see Orders). Optional header `Idempotency-Key`.

Checkout error codes: `ERR_RES_001` (404), `ERR_CHK_001` Expired (409), `ERR_CHK_002` InvalidState (409), `ERR_CHK_005` Unauthorized (401), `ERR_CHK_003` CartEmpty (409), `ERR_CHK_004` ShippingMethodUnsupported (400), `ERR_STK_001` InsufficientStock (409, with `lines` metadata), `ERR_IDP_001` IdempotencyKeyReuse (409), `ERR_ORD_001` OrderNotFound (404).

**Guest checkout — `/api/v1/guest-checkout`:** same response shapes. `POST /guest-checkout` → 201 `CheckoutResponse`; `POST /guest-checkout/{checkoutId}/place` → 200 `OrderResponse`; `GET /guest-checkout/orders?email=` → 200 `OrderResponse[]`.

---

## Orders — `/api/v1/orders` (`[Authorize]`)

`OrderResponse` (Detail, Cancel, Place):

```json
{
  "orderId": "guid", "orderNumber": "ORD-10001", "checkoutId": "guid", "cartId": "guid", "customerId": "guid|null", "customerEmail": "a@b.com",
  "currency": "USD", "subtotal": 100.0, "itemDiscount": 10.0, "cartDiscount": 5.0, "shippingTotal": 9.9, "taxTotal": 4.0, "grandTotal": 98.9,
  "status": "Placed", "placedAt": "2026-08-28T10:00:00Z",
  "lines": [ { "productId": "guid", "sku": "SKU-1", "name": "X", "unitPrice": 90.0, "quantity": 1, "imageUrl": null } ],
  "timeline": [ { "fromStatus": null, "toStatus": "Placed", "actorType": "Customer", "actorId": "guid", "traceId": "abc", "occurredAt": "2026-08-28T10:00:00Z" } ],
  "backorderedItems": [ { "orderBackorderItemId": "guid", "productId": "guid", "sku": "SKU-2", "quantity": 3, "filledQuantity": 1, "status": "Open", "createdAt": "datetime", "filledAt": null } ]
}
```

`status` values: `Pending`, `Placed`, `AwaitingPayment`, `Paid`, `Backordered`, `AwaitingFulfillment`, `Picking`, `Packed`, `Shipped`, `Delivered`, `Completed`, `Cancelled`.

`OrderHistoryResponse` (`GET /orders?cursor=&pageSize=`):

```json
{ "items": [ { "orderId": "guid", "orderNumber": "ORD-1", "status": "Placed", "grandTotal": 98.9, "currency": "USD", "placedAt": "datetime|null", "lineCount": 1 } ], "nextCursor": "abc123", "hasNext": true, "pageSize": 20 }
```

| Method | Path | Success |
|---|---|---|
| GET | `/orders?cursor=&pageSize=` | 200 `OrderHistoryResponse` (+ `Link` header when `nextCursor`) |
| GET | `/orders/{orderNumber}` | 200 `OrderResponse` |
| GET | `/orders/{orderNumber}/timeline` | 200 `OrderTimelineResponse[]` |
| POST | `/orders/{orderNumber}/cancel` | 200 `OrderResponse` |
| POST | `/orders/{orderNumber}/reorder` | 200 `CartResponse` |

Order error codes: `ERR_ORD_001` NotFound (404), `ERR_ORD_002` InvalidState (409), `ERR_ORD_003` NotYourOrder (403), `ERR_ORD_004` CancellationNotAllowed (409), `ERR_IDP_001` IdempotencyKeyReuse (409).

---

## Payments — `/api/v1/payments` (`[Authorize]` + `[Idempotent]`)

> **Idempotency-Key header is REQUIRED** on every POST here. Missing it → 400 `{"error":"Idempotency-Key header is required for this operation."}`. Successful responses are cached 24h in Redis and returned on retry with 200.

`POST /payments/{paymentId}/authorize` → 200 `PaymentResponse`:

```json
{
  "paymentId": "guid", "currency": "USD", "amount": 129.99,
  "status": 1,
  "providerKey": "stripe", "providerReference": "pi_3PxYZ...", "clientToken": "pm_test_abc123",
  "authorizedAt": "2026-08-28T14:30:00Z", "attempt": 1, "retryAfterUtc": null
}
```

`status` numeric enum: `0` Created, `1` Authorized, `2` Failed, `3` RetryPending, `4` Captured, `5` Cancelled, `6` Refunding, `7` Refunded, `8` RefundFailed.

Payment error codes: `ERR_RES_001` (404), `ERR_PAY_001` Declined (402), `ERR_PAY_002` CaptureConflict (409), `ERR_PAY_003` NotAuthorized (409), `ERR_PAY_004` ProviderUnavailable (502), `ERR_PAY_005` RefundNotAllowed (409), `ERR_PAY_006` RetryInCooldown (409), `ERR_PAY_007` RetryExhausted (402).

---

## Refunds — `/api/v1`

`POST /orders/{orderNumber}/refunds` → **201** + `Location` header. Optional `Idempotency-Key`. Body `RefundResponse`:

```json
{
  "refundId": "guid", "orderId": "guid", "paymentId": "guid", "amount": 29.5, "currency": "USD",
  "reason": "Damaged goods", "restock": true, "status": "requested",
  "providerReference": null, "refundableAmount": 100.49, "idempotencyKey": "refund-req-7788"
}
```

`status` (lowercase): `requested`, `approved`, `rejected`, `executing`, `completed`, `failed`.

- `POST /refunds/{refundId}/approve` → 200 `RefundResponse` · `POST /refunds/{refundId}/execute` → 200 `RefundResponse`
- Codes: `ERR_PAY_008` NotFound (404), `ERR_PAY_002` InvalidState (409), `ERR_PAY_003` ExceedsRefundable (409), `ERR_PAY_009` IdempotencyKeyReuse (409), `ERR_PAY_011` NotApproved (409), `ERR_RES_002` OrderNotFound (404).

---

## Wallets — `/api/v1/wallets` (`[Authorize]` + `[Idempotent]`)

- `GET /wallets/me` → 200 `WalletResponse`:

```json
{ "balance": 150.25, "currency": "USD", "loyaltyPoints": 125 }
```

(Returns a zero wallet `{balance:0,...}` when none exists — not an error.)

- `POST /wallets/deposit` (body `{ "amount": number }`) → 200 empty. **Requires `Idempotency-Key`.**
- `POST /wallets/convert-points` (body `{ "points": int }`) → 200 empty.
- Codes: `Wallet.NotFound` (404), `Wallet.InsufficientFunds` (422), `Wallet.InsufficientPoints` (422).

---

## Stock / Warehouses

### Stock — `/api/v1/stock`

- `GET /stock?page=&pageSize=&warehouseId=` → 200 `PagedStockItemsResponse`:

```json
{ "items": [ { "id": "guid", "sku": "SKU-1", "warehouseId": "guid", "onHand": 10, "allocated": 2, "available": 8 } ], "page": 1, "pageSize": 20, "totalCount": 0 }
```

- `GET /stock/{stockItemId}` → 200 single item. `Stock.StockItemNotFound` (404).
- `GET /stock/movements?stockItemId=&page=&pageSize=` → 200 `PagedStockMovementsResponse`:

```json
{ "items": [ { "id": "guid", "stockItemId": "guid", "type": "string", "quantity": 1, "onHandDelta": 1, "allocatedDelta": 0, "reason": "string", "reference": "string|null", "note": "string|null", "createdAt": "datetime" } ], "page": 1, "pageSize": 20, "totalCount": 0 }
```

- `POST /stock/movements` → 204 · `POST /stock/transfers` → 204

### Warehouses — `/api/v1/warehouses`

- `GET /warehouses?page=&pageSize=` → 200 `PagedWarehousesResponse` — note **`status` is numeric** (`0` Active, `1` Inactive):

```json
{ "items": [ { "id": "guid", "code": "WH1", "name": "Main", "address": "string", "timezone": "string", "status": 0 } ], "page": 1, "pageSize": 20, "totalCount": 0 }
```

- `GET /warehouses/{id}` → 200 single · `POST /warehouses` → 201 `{ "id": "guid" }` · `PATCH /warehouses/{id}` → 204 · `DELETE /warehouses/{id}` → 204
- Codes: `Warehouse.WarehouseNotFound` (404), `Warehouse.CodeAlreadyExists` (409).

---

## Fulfillment / Shipments — `/api/v1/fulfillment`, `/api/v1/shipments`

`FulfillmentTaskResponse`:

```json
{
  "taskId": "guid", "orderId": "guid", "warehouseId": "guid", "parentTaskId": "guid|null", "zone": "string|null", "priority": 0,
  "status": "Queued", "assignedTo": "guid|null", "assignedAt": "datetime|null", "startedAt": "datetime|null",
  "packedAt": "datetime|null", "shippedAt": "datetime|null", "cancelledAt": "datetime|null", "cancellationReason": "string|null",
  "createdAt": "datetime", "updatedAt": "datetime",
  "items": [ { "id": "guid", "productId": "guid", "sku": "string", "quantity": 1, "binLocation": "string|null" } ]
}
```

`status` values: `Queued`, `Assigned`, `Picking`, `Packed`, `Shipped`, `Cancelled`.

| Method | Path | Success |
|---|---|---|
| POST | `/fulfillment/tasks` | 201 + Location |
| GET | `/fulfillment/tasks?warehouseId=&status=&page=&pageSize=` | 200 `{ items, page, pageSize, totalCount }` |
| GET | `/fulfillment/tasks/{taskId}` | 200 |
| POST | `/fulfillment/tasks/{taskId}/assign` | 200 |
| POST | `/fulfillment/tasks/{taskId}/start-picking` | 200 |
| POST | `/fulfillment/tasks/{taskId}/pack` | 200 |
| POST | `/fulfillment/tasks/{taskId}/split` | 200 |
| GET | `/fulfillment/pick-lists?warehouseId=` | 200 `PickListResponse[]` |
| POST | `/fulfillment/shipments` | 200 `ShipmentResponse` |
| GET | `/fulfillment/shipping-rates/quote?...` | 200 `RateQuoteResponse` |
| PUT | `/fulfillment/orders/{orderId}/shipping-address` | 204 |

`ShipmentResponse`:

```json
{ "shipmentId": "guid", "orderId": "guid", "fulfillmentTaskId": "guid", "carrierKey": "string", "trackingNumber": "string", "labelUrl": "string|null", "status": "Created", "shippedAt": "datetime|null", "deliveredAt": "datetime|null", "updates": [ { "id": "guid", "status": "Created", "occurredAt": "datetime", "note": "string|null" } ] }
```

`ShipmentStatus`: `Created`, `InTransit`, `OutForDelivery`, `Delivered`, `Exception`.

- `GET /shipments/{shipmentId}` → 200 · `POST /shipments/{shipmentId}/tracking` → 200 (updated)
- Codes: `ERR_SHP_001` (404), `ERR_SHP_002/003` InvalidTransition (409), `ERR_FLM_001` TaskNotFound (404), `ERR_FLM_010` CarrierUnavailable (502).

---

## Returns — `/api/v1/returns` (`[Authorize]`)

- `POST /returns` → **201**, body is a **raw GUID string** (the return request id): `"3fa85f64-..."`.
- `GET /returns/{returnId}` → 200 `ReturnRequestResponse`:

```json
{ "id": "guid", "orderId": "guid", "reason": "string", "currency": "USD", "refundAmount": 0.0, "restock": false, "status": "Requested", "adminNotes": "string|null", "createdAt": "datetime" }
```

`status` values: `Requested`, `Approved`, `Rejected`, `Completed`, `Cancelled`.

- `GET /returns/order/{orderId}` → 200 `ReturnRequestResponse[]`
- `POST /returns/{returnId}/approve` → 200 `{ "returnId": "guid", "status": "approved" }` · `POST /returns/{returnId}/reject` → 200 `{ "returnId": "guid", "status": "rejected" }`

> Note: return errors map to 500 (no `ErrorType` defined): `ReturnRequest.OrderNotFound`, `ReturnRequest.NotFound`, `ReturnRequest.InvalidStatus`.

---

## Invoices — `/api/v1/invoices`

- `GET /invoices?status=&page=&pageSize=` → 200 `PagedInvoicesResponse`. **`status` is numeric** (`0` Issued, `1` Paid, `2` PartiallyRefunded, `3` Refunded, `4` Cancelled):

```json
{
  "items": [ { "invoiceId": "guid", "invoiceNumber": "INV-1", "orderId": "guid", "customerId": "guid|null", "currency": "USD", "status": 0, "taxRate": 0.14, "taxAmount": 14.0, "total": 114.0, "creditedTotal": 0.0, "pdfUrl": "string|null", "issuedAt": "datetime", "lines": [ { "id": "guid", "sku": "SKU", "description": "string", "quantity": 1, "unitAmount": 100.0, "taxRate": 0.14, "amount": 114.0 } ] } ],
  "totalCount": 0, "page": 1, "pageSize": 20
}
```

- `GET /invoices/{invoiceId}` → 200 single · `GET /invoices/{invoiceId}/pdf` → 200 binary PDF · `GET /invoices/{invoiceId}/credit-notes` → 200 `PagedCreditNotesResponse`
- Code: `ERR_INV_001` InvoiceNotFound (404).

---

## Exports — `/api/v1/exports` (async jobs)

- `POST /exports` → **202** + `Location`. Body `ExportStartedResponse`: `{ "exportId": "guid", "status": "Queued" }`. Body `reportType`: `sales|inventory|finance`, plus `from`, `to`, `granularity` (`day|week|month`), `currency`.
- `GET /exports/{exportId}` → 200 `ExportStatusResponse`:

```json
{ "exportId": "guid", "reportType": "sales", "status": "Completed", "rowCount": 10, "fileKey": "exports/xxx.csv", "createdBy": "guid|null", "createdAt": "datetime", "startedAtUtc": "datetime|null", "completedAtUtc": "datetime|null" }
```

`status` values: `Queued`, `Running`, `Completed`, `Failed`.

- `GET /exports/{exportId}/download` → 200 CSV file (`text/csv`). Codes: `Reporting.ExportNotReady` (409), `Reporting.ExportFileMissing` (404), `Reporting.ExportNotFound` (404).

---

## Reconciliation — `/api/v1/reconciliation` (`[Authorize]`, `finance.reconcile`)

`POST /reconciliation/run` → 200 `ReconciliationRunResponse`:

```json
{
  "matchedCount": 42, "driftCount": 2, "unmatchedCount": 1, "providerOnlyCount": 0,
  "drifts": [ { "recordId": "guid", "paymentId": "guid", "providerReference": "string", "status": 2, "detail": "string" } ],
  "checkedAtUtc": "2026-08-28T14:30:00Z"
}
```

`status` numeric enum: `0` Pending, `1` Matched, `2` Drift, `3` Unmatched.

---

## Reports — `/api/v1/reports` (`[Authorize]`, `reports.read`)

- `GET /reports/sales?from=&to=&granularity=&currency=` → 200 `SalesReportResponse`:

```json
{ "from": "datetime", "to": "datetime", "granularity": "day", "currency": "usd", "totals": { "orders": 120, "revenue": 5400.50, "items": 300 }, "series": [ { "periodStart": "datetime", "orders": 5, "revenue": 220.0, "items": 12 } ] }
```

- `GET /reports/inventory` → 200 `InventoryReportResponse` (totals + per-warehouse lines)
- `GET /reports/finance?from=&to=` → 200 `FinanceReportResponse` (per-currency totals)
- `GET /reports/promotions?from=&to=` → 200 `PromotionReportResponse`
- `GET /reports/fulfillment?from=&to=` → 200 `FulfillmentReportResponse`

---

## Audit — `/api/v1/audit-logs` (`[Authorize]`, `AuditRead`)

`GET /audit-logs?actorId=&action=&entityType=&from=&to=&page=&pageSize=` → 200 `PagedAuditLogsResponse`:

```json
{ "items": [ { "id": 1, "actorId": "guid", "action": "order.updated", "entityType": "Order", "entityId": "ord_123", "before": "{\"status\":\"pending\"}", "after": "{\"status\":\"paid\"}", "ip": "192.168.1.1", "traceId": "00-abc", "hash": "sha256hex", "previousHash": "sha256hex", "occurredAt": "datetime" } ], "page": 1, "pageSize": 20, "totalCount": 42 }
```

(`before`/`after` are JSON strings; `pageSize` clamped 1–100.)

---

## Webhooks — `/api/v1/webhooks`, `/api/v1/webhook-endpoints` (`integrations.*`)

- `POST /webhooks/replay` → 200 `{ "replayed": 1 }`
- `POST /webhook-endpoints` → **201** `WebhookEndpointCreatedResponse` (secret shown **once**):

```json
{ "endpointId": "guid", "name": "My Webhook", "url": "https://example.com/hooks", "secret": "whsec_...", "eventTypes": ["order.placed", "order.paid"] }
```

- `GET /webhook-endpoints` → 200 `[{ "endpointId": "guid", "name": "string", "url": "string", "isActive": true, "suspendedUntilUtc": null, "eventTypes": ["order.placed"] }]`
- `POST /webhook-endpoints/{endpointId}/rotate-secret` → 200 `{ "endpointId": "guid", "secret": "whsec_..." }`
- `GET /webhook-endpoints/{endpointId}/deliveries?limit=` → 200 `WebhookDeliveryDto[]`
- `POST /webhook-endpoints/{endpointId}/replay` → 200 `{ "replayed": 1 }`
- Codes: `Integrations.EndpointNotFound` (404), `Integrations.DeliveryNotFound` (404).

### Dead-letter — `/api/v1/webhooks/dead-letter` (no auth)

- `GET /webhooks/dead-letter?limit=&offset=&eventType=` → 200 `{ "total": 12, "offset": 0, "limit": 50, "entries": [ { "id": "guid", "deliveryId": "guid", "endpointId": "guid", "eventType": "order.placed", "eventId": "evt_", "payloadJson": "{}", "endpointUrl": "https://...", "endpointName": "string", "totalAttempts": 5, "lastStatusCode": 500, "errorReason": "HTTP 500", "firstFailedAtUtc": "datetime", "lastFailedAtUtc": "datetime", "isReplayed": false, "replayedAtUtc": null } ] }`
- `GET /webhooks/dead-letter/{id}` → 200 single · `POST /webhooks/dead-letter/{id}/replay` → 200 `{ "message": "Delivery replayed.", "deliveryId": "guid" }` · `GET /webhooks/dead-letter/stats` → 200 `{ "total": 12, "byEventType": { "order_placed": 4 } }`

> This controller returns plain JSON/`NotFound` bodies, **not** ProblemDetails.

---

## Support & Notifications

### Support orders — `/api/v1/support/orders` (`[Authorize]`, `orders.support.read`)

- `GET /support/orders?orderNumber=&email=&customerId=` → 200 `SupportOrderLookupResponse`:

```json
{ "orders": [ { "orderId": "guid", "orderNumber": "ORD-1001", "customerId": "guid|null", "maskedEmail": "j***@example.com", "status": "PendingPayment", "grandTotal": 250.0, "currency": "usd", "placedAt": "datetime|null" } ] }
```

- Code: `SupportLookup.EmptyFilters` (400) when no filter provided.

### Tenant billing — `/api/v1/billing` (`[Authorize]`, role `Admin`)

- `GET /billing/summary` → 200 `BillingSummaryResponse`:

```json
{ "planName": "Business", "monthlyPrice": 49.0, "status": "Active", "currentPeriodEnd": "datetime|null", "currentProducts": 123, "maxProducts": 500, "supportsCustomDomain": true, "advancedAnalytics": true }
```

- `POST /billing/change-plan` (body `{ "planId": "guid" }`) → 200 (raw GUID string)
- Errors use `400 BadRequest(result.Error)` — **not** ProblemDetails. Codes: `Tenant.Unauthorized`, `Tenant.NotFound`, `Subscription.PlanNotFound`.

### Notification preferences — `/api/v1/me/notifications` (`[Authorize]`)

- `GET /me/notifications/preferences` → 200 `[{ "id": "guid", "channel": "Email", "kind": "OrderConfirmation", "enabled": true }]`
- `PUT /me/notifications/preferences/{channel}/{kind}` (body `{ "enabled": bool }`) → 204
- Code: `Notifications.InvalidChannelOrKind` (400).

### Platform tenants — `/api/v1/platform/tenants` (role `SuperAdmin`)

- `GET /platform/tenants` → 200 `TenantResponse[]` `[{ "id": "guid", "name": "Acme", "subdomain": "acme", "customDomain": "acme.com|null", "status": "Active" }]`
- `POST /platform/tenants/{tenantId}/suspend|activate` → 200 empty. Errors return raw `Error` object (404) — **not** ProblemDetails: `{ "code": "Tenant.NotFound", "message": "...", "type": 2 }`.

---

## Stripe webhooks (public)

- `POST /api/v1/webhooks/stripe` — header `Stripe-Signature` required. 401 on invalid. Always **200 empty body** on success.
- `POST /api/v1/webhooks/stripe/subscription` — same behavior. 400 on malformed body.

---

## Important Global Notes

1. **Enums-as-numbers:** `Product.status`, `Warehouse.status`, `Invoice.status`, `Payment.status`, and `Reconciliation drift.status` serialize as **integers**. Most other statuses are strings (see each section).
2. **Idempotency-Key:** required on `POST /payments/*/authorize`, `POST /wallets/deposit`, `POST /wallets/convert-points`; optional on checkout place and refund create. Missing on required endpoints → 400.
3. **401 on default:** unauthenticated requests to `[Authorize]` endpoints produce 401 (often empty body / no ProblemDetails).
4. **Media endpoints:** invoice PDF and export download return binary files, not JSON.
