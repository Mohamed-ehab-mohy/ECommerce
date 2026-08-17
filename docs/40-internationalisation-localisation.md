# Document 40 — Internationalisation & Localisation

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** Internationalisation & Localisation Strategy
> **Status:** Draft v1.0
> **Audience:** Engineering, Product, UX

---

## 1. Overview

The platform supports international commerce through three dimensions of localisation: **multi-currency pricing**, **locale-aware content**, and **product translation**. All monetary values are stored with a currency code, customer locale preferences are persisted on the customer entity, and product names/descriptions can be translated per locale.

---

## 2. Multi-Currency: Money Value Object

### 2.1 Definition

**File:** `Domain/Pricing/Money.cs`

`Money` is a `readonly record struct` that enforces currency-safe monetary operations:

```csharp
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    public decimal DisplayAmount => decimal.Round(Amount, DisplayPrecision, MidpointRounding.AwayFromZero);
}
```

### 2.2 Storage vs Display Precision

| Property | Precision | Rounding | Purpose |
|----------|-----------|----------|---------|
| `Amount` | 4 decimal places | `MidpointRounding.AwayFromZero` | Internal storage — prevents rounding drift in calculations |
| `DisplayAmount` | 2 decimal places | `MidpointRounding.AwayFromZero` | User-facing display — standard currency formatting |

### 2.3 Factory Method

```csharp
public static Money From(decimal amount, string currency)
```

- Validates that `currency` is not null/empty/whitespace
- Trims and uppercases the currency code (e.g., `" usd "` → `"USD"`)
- Rounds `amount` to 4 decimal places for storage

### 2.4 Currency Conversion

```csharp
public Money ConvertTo(string targetCurrency, decimal rate)
```

Multiplies `Amount` by the exchange `rate` and produces a new `Money` in the target currency. The caller is responsible for providing the correct rate (e.g., from an exchange rate service).

### 2.5 String Representation

```csharp
public override string ToString() => $"{DisplayAmount:F2} {Currency}";
// Example: "49.99 USD"
```

### 2.6 Currency Field Across Entities

The `Currency` string field appears on multiple domain entities to support multi-currency pricing:

| Entity | Currency Context |
|--------|-----------------|
| `ProductPrice` | Currency for the list/override price |
| `Cart` | Currency for the cart session |
| `CartItem` | Inherited from cart |
| `Order` | Currency for the order total |
| `Payment` | Currency for the payment amount |
| `Refund` | Currency for the refund amount |
| `Invoice` / `CreditNote` | Currency for the invoice total |
| `Customer` | Preferred display currency |

---

## 3. Locale: Customer.Locale

### 3.1 Customer Entity

**File:** `Domain/Identity/Customer.cs:26`

```csharp
public string Locale { get; private set; }
```

Each customer stores their preferred locale (e.g., `"en"`, `"ar"`, `"fr"`). This locale is set during registration and can be updated via the profile API.

### 3.2 Registration

**File:** `API/Controllers/AuthRequests.cs:3`

```csharp
public sealed record RegisterRequest(
    string Email, string Password, string DisplayName,
    string Locale, string Currency);
```

Locale and currency are collected at registration time, linking the customer's regional preferences to their account.

### 3.3 Profile Update

**File:** `API/Controllers/ProfileRequests.cs:3`

```csharp
public sealed record UpdateProfileRequest(
    string? DisplayName, string? Phone, string? Locale, string? Currency);
```

Customers can change their locale and currency preferences after registration.

### 3.4 Supported Locales

**File:** `UseCases/Pricing/DefaultLocaleCatalog.cs:9`

```csharp
public IReadOnlyList<string> SupportedLocales { get; } =
    ["en", "ar", "fr", "de", "es", "it", "pt", "tr", "ru", "zh"];
public string DefaultLocale { get; } = "en";
```

The platform supports 10 locales with English as the default fallback.

---

## 4. Product Translations

### 4.1 ProductTranslation Entity

**File:** `Domain/Catalog/ProductTranslation.cs`

A translation is a child entity of `Product`, keyed by `(ProductId, Locale)`:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `ProductId` | `Guid` | Yes | Parent product |
| `Locale` | `string` | Yes | Locale code (e.g., `"en"`, `"ar"`) |
| `Name` | `string` | Yes | Translated product name |
| `Description` | `string?` | No | Translated description |
| `MetaTitle` | `string?` | No | SEO meta title in target language |
| `MetaDescription` | `string?` | No | SEO meta description in target language |

### 4.2 Factory and Update

```csharp
// Create a new translation
ProductTranslation.Create(productId, "ar", "قميص قطن", "قميص قطن مريح", null, null);

// Update an existing translation
translation.Update("قميص قطن محدث", "Description updated", null, null);
```

### 4.3 Querying Translations

Translations are queried per locale. The `GetProductQuery` accepts an optional `Locale` parameter:

```csharp
public sealed record GetProductQuery(Guid ProductId, string? Locale = null, string? Currency = null)
```

When `Locale` is specified, the product name and description are resolved from `ProductTranslations`. When null, the base product fields are returned.

### 4.4 ProductSearchDocument Locale

**File:** `Infrastructure/Search/ProductSearchDocument.cs:9`

```csharp
public string Locale { get; set; } = string.Empty;
```

Search documents are locale-aware, enabling locale-filtered full-text search via the `pg_trgm` PostgreSQL extension.

### 4.5 Database Index

The `ProductTranslations` table has an index on the `Locale` column (`ix_product_translations_locale`), enabling efficient per-locale lookups.

---

## 5. Date/Time: UTC Storage Convention

### 5.1 Convention

All date/time values in the domain are stored as **UTC** (`DateTime` with `Kind = Utc`). This is enforced by:

- Using `timeProvider.GetUtcNow().UtcDateTime` instead of `DateTime.UtcNow` (enables testability via `TimeProvider`)
- Storing `CreatedAt`, `UpdatedAt`, `ExpiresAt`, `PlacedAt`, `OccurredAtUtc` fields as UTC throughout all entities

### 5.2 Examples

| Entity | UTC Fields |
|--------|------------|
| `Cart` | `CreatedAt`, `UpdatedAt`, `ExpiresAt` |
| `Order` | `PlacedAt` |
| `AuditEntry` | `OccurredAtUtc` |
| `InMemoryShippingRateCache.Entry` | `ExpiresAtUtc` |
| `InMemoryLoginAttemptThrottler` | `WindowStartUtc` |

### 5.3 Display

UTC values are converted to the customer's local time zone only at the presentation layer (API response formatting or frontend rendering). They are never stored in local time.

---

## 6. Number Formatting: Money.DisplayAmount

### 6.1 Display Pipeline

```
Raw Amount (4 decimals) → DisplayAmount (2 decimals) → ToString() → "49.99 USD"
```

`Money.DisplayAmount` rounds the stored `Amount` to 2 decimal places using `MidpointRounding.AwayFromZero` (standard banker's rounding is avoided in favour of symmetric rounding for financial clarity).

### 6.2 Locale-Aware Formatting

The `ToString()` method produces a machine-readable format (`"49.99 USD"`). For user-facing display, the API and frontend apply locale-specific formatting:

- `$49.99` (en-US)
- `49,99 €` (de-DE)
- `٤٩٫٩٩ ر.س` (ar-SA)

This formatting is handled at the presentation layer, not in the domain model.

---

## 7. Future Roadmap

| Feature | Priority | Description |
|---------|----------|-------------|
| Translation management UI | High | Admin dashboard for managing `ProductTranslation` records |
| Auto-locale detection | Medium | Detect browser/geo locale and set customer locale on first visit |
| RTL layout support | High | Full right-to-left layout for Arabic and Hebrew locales |
| Exchange rate service integration | High | Replace manual `ConvertTo` calls with a cached exchange rate API |
| Currency-specific rounding rules | Medium | Different rounding rules per currency (e.g., JPY = 0 decimals) |
| Pluralisation support | Low | ICU MessageFormat for locale-aware plural forms in notifications |
| Date/number format presets | Medium | Locale-specific date formats (`dd/MM/yyyy` vs `MM/dd/yyyy`) and number separators |
