# 13 — Cart & Wishlist Module Design

## Overview

The Cart module manages per-user (or per-anonymous-session) shopping carts with item CRUD, quantity validation, coupon application, and price snapshot detection. The Wishlist module provides authenticated users with a saved-for-later product list, including a move-to-cart operation. Both modules use an `OwnerKey` abstraction that unifies logged-in (`user:{userId}`) and anonymous (`anon:{key}`) ownership.

## Domain Entities

### Cart (`Domain/Cart/Cart.cs:7`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Cart` | `OwnerKey`, `Currency`, `Version` (optimistic concurrency), `ExpiresAt`, `AppliedCouponCode`, `Items` | Aggregate root |
| `CartItem` | `CartId`, `ProductId`, `Sku`, `Name`, `ListPrice`, `UnitPrice`, `Quantity`, `ImageUrl`, `UpdatedAt` | Price snapshot at add-time |

### Wishlist (`Domain/Wishlist/Wishlist.cs:7`)

| Entity | Key Properties | Notes |
|---|---|---|
| `Wishlist` | `OwnerKey`, `Items` | One per user |
| `WishlistItem` | `WishlistId`, `ProductId`, `AddedAt` | Only stores product reference |

### CartItem

- Quantity bounded to `[1, 99]`.
- `UnitPrice` must be `≤ ListPrice` and `≥ 0` — enforced by `Cart.AddItem()` at `Cart.cs:92`.
- Duplicate product adds merge quantity instead of creating a new line.

## Key Operations

| Operation | Domain Method | Description |
|---|---|---|
| Create cart | `Cart.Create()` | New cart with expiry and currency |
| Rehydrate cart | `Cart.Rehydrate()` | Rebuild from persistence with items |
| Add item | `Cart.AddItem()` | Upsert: merge if product exists, else create. Emits `CartItemAdded` |
| Update quantity | `Cart.UpdateQuantity()` | Set absolute quantity for a product line |
| Remove item | `Cart.RemoveItem()` | Remove by product ID. Emits `CartItemRemoved` |
| Apply coupon | `Cart.ApplyCoupon()` | Stores uppercase code. Emits `CartCouponApplied` |
| Remove coupon | `Cart.RemoveCoupon()` | Clears coupon. Emits `CartCouponRemoved` |
| Merge carts | `Cart.MergeFrom()` | Merge anonymous into authenticated cart, newer-wins for quantity conflicts. Emits `CartMerged` |
| Check expiry | `Cart.IsExpired()` | `ExpiresAt ≤ now` |
| Price changes | `GetCartPriceChangesQuery` | Detects stale prices vs. current catalog |

### Wishlist Operations

| Operation | Domain Method | Description |
|---|---|---|
| Create wishlist | `Wishlist.Create()` | Owner-key scoped |
| Add item | `Wishlist.AddItem()` | Idempotent add. Emits `WishlistItemAdded` |
| Remove item | `Wishlist.RemoveItem()` | Emits `WishlistItemRemoved` |
| Move to cart | `MoveWishlistItemToCartCommand` | Removes from wishlist, adds to cart |

## API Endpoints

### Cart — `CartController.cs:12` — `/api/v1/carts/me`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/carts/me` | Get or create cart; returns `X-Cart-Key` header |
| `POST` | `/api/v1/carts/me/items` | Add item to cart |
| `PATCH` | `/api/v1/carts/me/items/{productId}` | Update item quantity |
| `DELETE` | `/api/v1/carts/me/items/{productId}` | Remove item |
| `GET` | `/api/v1/carts/me/price-changes` | Detect price changes since last view |
| `POST` | `/api/v1/carts/me/coupons` | Apply coupon code |
| `DELETE` | `/api/v1/carts/me/coupons` | Remove applied coupon |

Anonymous carts use the `X-Cart-Key` header for identification. Authenticated users resolve `OwnerKey` as `user:{userId}`.

### Wishlist — `WishlistController.cs:14` — `/api/v1/wishlist`

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/wishlist` | Get wishlist (requires auth) |
| `POST` | `/api/v1/wishlist/items` | Add product to wishlist |
| `DELETE` | `/api/v1/wishlist/items/{productId}` | Remove from wishlist |
| `POST` | `/api/v1/wishlist/items/{productId}/move` | Move item to cart |

## Integration Points

- **Checkout**: `InitiateCheckoutCommand` consumes the cart's items and coupon. The checkout freezes a `PriceSnapshot` from cart line prices.
- **Promotions/Pricing**: `ApplyCartCouponCommandHandler` validates the coupon through the pricing engine before storing the code.
- **Product Catalog**: `AddCartItemCommandHandler` fetches current product name, SKU, and prices from `IProductRepository` to snapshot into the cart item.
- **Cart Merge**: `CartMergeService` merges anonymous carts into authenticated carts on login, using `Cart.MergeFrom()`.
- **Domain Events**: `CartItemAdded`, `CartItemRemoved`, `CartCouponApplied`, `CartCouponRemoved`, `CartMerged`, `CartExpired`.
- **Concurrency**: Cart `Version` field enables optimistic concurrency for concurrent cart modifications.

## File References

| File | Purpose |
|---|---|
| `src/ECommerce.Domain/Cart/Cart.cs` | Cart aggregate root with business rules |
| `src/ECommerce.Domain/Cart/CartItem.cs` | Cart line item with price snapshot |
| `src/ECommerce.Domain/Cart/CartErrors.cs` | Domain error constants |
| `src/ECommerce.Domain/Cart/CartConcurrencyException.cs` | Concurrency conflict type |
| `src/ECommerce.Domain/Wishlist/Wishlist.cs` | Wishlist aggregate root |
| `src/ECommerce.Domain/Wishlist/WishlistItem.cs` | Wishlist line item |
| `src/ECommerce.Domain/Wishlist/WishlistErrors.cs` | Wishlist error constants |
| `src/ECommerce.API/Controllers/CartController.cs` | Cart REST API |
| `src/ECommerce.API/Controllers/WishlistController.cs` | Wishlist REST API |
| `src/ECommerce.UseCases/Cart/Handlers/*.cs` | Cart command/query handlers |
| `src/ECommerce.UseCases/Cart/Services/CartMergeService.cs` | Anonymous-to-auth cart merge |
| `src/ECommerce.UseCases/Wishlist/Handlers/*.cs` | Wishlist command/query handlers |
