# 12 — Catalog & Search Module Design

## Overview

The Catalog module manages the product catalog including products, categories, brands, product variants, and multi-currency pricing. It supports full-text search, faceted filtering (by category, brand, price range, rating), and paginated listing. Products carry i18n translations and multi-currency price records.

## Domain Entities

| Entity | Key Properties | Source |
|---|---|---|
| `Product` | `Sku`, `Slug`, `CategoryId`, `BrandId`, `Status` (Draft/Active/Inactive), `IsFeatured`, `Backorderable`, `ImageUrls`, `Attributes` | `Domain/Catalog/Product.cs:6` |
| `ProductTranslation` | `Locale`, `Name`, `Description` | `Domain/Catalog/ProductTranslation.cs` |
| `ProductPrice` | `Currency`, `ListAmount`, `OfferAmount` | `Domain/Catalog/ProductPrice.cs:3` |
| `Category` | `Name`, `Slug`, `ParentId`, `SortOrder`, `Level` | `Domain/Catalog/Category.cs:5` |
| `Brand` | `Name`, `Description`, `Website` | `Domain/Catalog/Brand.cs:5` |
| `ProductVariant` | `ProductId`, `Sku`, `Name`, `Attributes` | `Domain/Catalog/ProductVariant.cs:5` |
| `ProductStatus` | `Draft`, `Active`, `Inactive` | `Domain/Catalog/ProductStatus.cs:3` |

`Product.Create()` accepts SKU, slug, locale, name, description, currency, list/offer amounts, category, brand, featured flag, status, and backorderable flag. It emits `ProductCreated` on creation and `ProductUpdated` / `ProductDeactivated` on changes. Deactivation sets `IsDeleted = true` (soft delete). Price and translation are managed via private `SetPrice` / `SetTranslation` methods that upsert per currency/locale.

## Key Operations

| Operation | Description | Handler |
|---|---|---|
| Create product | Create with SKU, translations, pricing, category/brand links | `CreateProductCommand` |
| Update product | Partial update of slug, name, prices, category, status | `UpdateProductCommand` |
| Deactivate product | Soft-delete: sets status Inactive + IsDeleted | `DeactivateProductCommand` |
| List products | Paginated listing with locale/currency resolution | `ListProductsQuery` → `ListProductsQueryHandler` |
| Search products | Full-text `q` + category/brand/price/rating filters | `SearchProductsQuery` → `SearchProductsQueryHandler` |
| Get product by ID | Single product with translations and prices | `GetProductQuery` |
| Import products | Bulk product creation via `ProductImportService` | `ProductImportsController` |
| Category tree | Hierarchical category navigation | `GetCategoryTreeQuery` |
| List brands | Paginated brand listing | `ListBrandsQuery` |

## API Endpoints

| Method | Route | Controller | Notes |
|---|---|---|---|
| `GET` | `/api/v1/products` | `ProductsController` | Dispatches to search or list based on query params |
| `GET` | `/api/v1/products/{id}` | `ProductsController` | Single product |
| `POST` | `/api/v1/products` | `ProductsController` | Admin, `[Authorize]` |
| `PATCH` | `/api/v1/products/{id}` | `ProductsController` | Admin, `[Authorize]` |
| `DELETE` | `/api/v1/products/{id}` | `ProductsController` | Soft-delete, `[Authorize]` |

Search query params: `q`, `categoryId`, `brandId`, `price.gte`, `price.lte`, `rating.gte`, `page`, `pageSize`, `locale`, `currency`.

Controller routes: `ProductsController.cs:11` — `/api/v1/products`

## Integration Points

- **Cart / Wishlist**: Cart items snapshot product name, SKU, and price at add-time. Wishlist stores only `ProductId`.
- **Checkout / Order**: `PriceSnapshot` freezes product prices at checkout; `OrderItem.FromSnapshot()` creates immutable order lines.
- **Inventory**: Stock tracked by SKU via `StockItem.Sku`. `Product.Backorderable` controls whether shortfalls are allowed at order placement.
- **Pricing Engine**: `PricingLine` carries `CategoryIds` and `BrandIds` for promotion targeting.
- **Product Search Port**: `IProductSearchRepository` abstracts the search index; `IProductRepository` for CRUD.
- **Domain Events**: `ProductCreated`, `ProductUpdated`, `ProductDeactivated` — dispatched via `IEventDispatcher`.

## File References

| File | Purpose |
|---|---|
| `src/ECommerce.Domain/Catalog/Product.cs` | Product aggregate root |
| `src/ECommerce.Domain/Catalog/ProductPrice.cs` | Multi-currency price value object |
| `src/ECommerce.Domain/Catalog/ProductVariant.cs` | SKU variant entity |
| `src/ECommerce.Domain/Catalog/Category.cs` | Hierarchical category |
| `src/ECommerce.Domain/Catalog/Brand.cs` | Brand entity |
| `src/ECommerce.Domain/Catalog/ProductStatus.cs` | Draft/Active/Inactive enum |
| `src/ECommerce.API/Controllers/ProductsController.cs` | REST API surface |
| `src/ECommerce.UseCases/Catalog/Handlers/*.cs` | MediatR command/query handlers |
| `src/ECommerce.UseCases/Catalog/Ports/IProductRepository.cs` | Repository port |
| `src/ECommerce.UseCases/Catalog/Ports/IProductSearchRepository.cs` | Search index port |
| `src/ECommerce.UseCases/Catalog/Services/ProductImportService.cs` | Bulk import service |
