# Document 03a — User Stories with Acceptance Criteria

> **Platform:** E-Commerce Platform (Working Title: `ECommerce`)
> **Document Type:** Product Backlog Specification (User Stories + Acceptance Criteria)
> **Status:** Draft v1.0 for review
> **Audience:** Product, Engineering, QA
> **Inputs:** `03-business-requirements.md`, `02a-user-personas.md`, `04a-functional-requirements-specification.md`
> **Relationship:** Stories are the agile decomposition of BRD requirements, written from persona perspectives. Each story traces to BRD/FRS IDs.

---

## 1. Document Control

| Version | Date       | Author / Owner | Change Summary                         |
|---------|------------|----------------|-----------------------------------------|
| 0.1     | 2026-07-20 | Product Owner  | Backlog workshop, initial story set     |
| 0.2     | 2026-07-28 | Product Owner  | Acceptance criteria, sizing, refinement |
| 1.0     | 2026-07-31 | Product Owner  | Baseline release                        |

### 1.1 Approvals

| Role         | Name | Decision | Date |
|--------------|------|----------|------|
| Product Owner | —    | —        | —    |
| Tech Lead     | —    | —        | —    |
| QA Lead       | —    | —        | —    |

---

## 2. Conventions

### 2.1 Story Format

> **As a** `<Persona>` **I want** `<goal>` **so that** `<benefit>`.

### 2.2 Fields

| Field | Meaning |
|-------|---------|
| ID | `US-<Epic>-<nnn>` |
| Priority | Must / Should / Could |
| Size | S (≤ 1 day), M (2–3 days), L (4–5 days), XL (> 5, split) |
| Ref | BRD / FRS identifiers |

### 2.3 Acceptance Criteria Style

Critical stories carry **Given/When/Then** scenarios. All stories carry a condensed acceptance checklist. A story is **Done** only when: implementation matches FRS behavior, validation/errors match the global error model (`FRS §3`), domain events + audit fire where required, and unit/integration tests pass.

### 2.4 Backlog Summary

| Epic | Stories | Must | Should | Could | Total Size (S/M/L units) |
|------|--------:|-----:|-------:|------:|--------------------------:|
| A. Identity & Account | 9 | 7 | 1 | 1 | 22 |
| B. Catalog & Search | 8 | 6 | 2 | 0 | 20 |
| C. Cart & Wishlist | 7 | 5 | 2 | 0 | 14 |
| D. Checkout & Orders | 9 | 7 | 2 | 0 | 26 |
| E. Pricing & Promotions | 8 | 6 | 2 | 0 | 19 |
| F. Inventory & Warehouses | 8 | 6 | 2 | 0 | 20 |
| G. Payments | 8 | 6 | 2 | 0 | 24 |
| H. Shipping & Fulfillment | 7 | 5 | 2 | 0 | 18 |
| I. Finance & Refunds | 7 | 5 | 2 | 0 | 18 |
| J. Notifications | 6 | 4 | 2 | 0 | 12 |
| K. Reviews | 6 | 4 | 1 | 1 | 11 |
| L. Analytics & Reporting | 7 | 4 | 3 | 0 | 16 |
| M. Platform & Governance | 8 | 6 | 2 | 0 | 22 |
| N. Real-time | 5 | 3 | 2 | 0 | 11 |
| **Total** | **103** | **74** | **27** | **2** | **253** |

---

## 3. Epic A — Identity & Account Management

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-A-001 | As **Ahmed**, I want to register with email and verify my email, so that my account is secure | Must | M | BR-1101, FRS-A-001 |
| US-A-002 | As **Ahmed**, I want to log in and stay logged in across sessions, so that I don't re-enter credentials | Must | M | BR-1102, FRS-A-002/003 |
| US-A-003 | As **Ahmed**, I want to reset my password, so that I can recover access | Must | S | BR-1103, FRS-A-004 |
| US-A-004 | As **Ahmed**, I want to manage my profile and addresses, so that checkout is fast | Must | M | BR-1104 |
| US-A-005 | As **Diego**, I want to look up a customer account, so that I can support them | Must | S | BR-1106 |
| US-A-006 | As **Ingrid**, I want to manage roles and permissions, so that access is least-privilege | Must | L | BR-1107, FRS-A-006/007 |
| US-A-007 | As **Ahmed**, I want to close my account, so that my data is erased per policy | Should | M | BR-1109, FRS-A-010 |
| US-A-008 | As **Ingrid**, I want to impersonate a customer under audit, so that I can reproduce issues | Could | L | BR-1110, FRS-A-009 |
| US-A-009 | As **Ahmed**, I want failed logins to lock me out safely, so that attackers can't brute-force me | Must | S | BR-1105, FRS-A-008 |

**US-A-001 — Register & Verify**
- Given I am a new guest with valid email/password, when I submit registration, then I receive 201 and a verification email.
- Given I submit a duplicate email, then I get 409 and no account change.
- Given I submit a weak password, then I get 422 with field-level errors.
- Given I am unverified, when I attempt order placement, then I am blocked (403) but can still browse.
- Given I click the verification link, then my account is verified and the token cannot be reused.

**US-A-002 — Login & Sessions**
- Given valid credentials, when I log in, then I receive an access token (15 min) and a refresh token.
- Given my refresh token, when I refresh, then a new pair is issued and the old refresh token is revoked.
- Given a reused (revoked) refresh token, then the whole family is revoked and a security alert fires.
- Given my account is locked, when I attempt login, then I get 423 with remaining lock time.

---

## 4. Epic B — Catalog & Search

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-B-001 | As **Elena**, I want to create and update products with unique SKUs, so that catalog stays clean | Must | M | BR-1201, FRS-B-001 |
| US-B-002 | As **Elena**, I want to manage categories and brands, so that navigation is organized | Must | M | BR-1202, FRS-B-002 |
| US-B-003 | As **Elena**, I want localized names and descriptions, so that all 10 locales are served | Must | M | BR-1203, FRS-B-003 |
| US-B-004 | As **Elena**, I want per-currency pricing, so that 5 currencies are correct | Must | M | BR-1204, FRS-B-004 |
| US-B-005 | As **Sarah**, I want to search products with typos tolerated, so that I find items fast | Must | L | BR-1205, FRS-B-005 |
| US-B-006 | As **Sarah**, I want to filter by category, brand, price, and rating, so that I narrow results | Must | M | BR-1206, FRS-B-006 |
| US-B-007 | As **Elena**, I want to bulk-import products, so that large updates take minutes not days | Should | L | BR-1207, FRS-B-007 |
| US-B-008 | As **Sarah**, I want unavailable products hidden, so that I never pick dead items | Must | M | BR-1209, FRS-B-009 |

**US-B-005 — Search**
- Given I type "phone" with typo "phne", when I search, then I get relevant phone results (p95 ≤ 300 ms).
- Given I search in Arabic, then results respect diacritics and locale ranking.
- Given I filter by price range and brand, then facets and results are consistent.
- Given an empty query, then I get 422.

---

## 5. Epic C — Cart & Wishlist

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-C-001 | As **Sarah**, I want my guest cart to persist, so that I can return to it | Must | M | BR-1301, FRS-C-001 |
| US-C-002 | As **Ahmed**, I want my cart and wishlist merged on login, so that nothing is lost | Must | M | BR-1302, FRS-C-002 |
| US-C-003 | As **Sarah**, I want to update quantities and remove items, so that I control my cart | Must | S | BR-1303, FRS-C-003 |
| US-C-004 | As **Sarah**, I want live totals, so that I see exactly what I'll pay | Must | M | BR-1304, FRS-C-004 |
| US-C-005 | As **Ahmed**, I want to be warned when a price changed, so that I'm not surprised at checkout | Should | M | BR-1305, FRS-C-005 |
| US-C-006 | As **Ahmed**, I want a wishlist, so that I can save items for later | Must | S | BR-1306, FRS-C-006 |
| US-C-007 | As **Sarah**, I want abandoned carts purged after expiry, so that data doesn't accumulate | Should | S | BR-1308, FRS-C-007 |

**US-C-002 — Merge on Login**
- Given I have a guest cart and log in with an existing customer cart, when the merge runs, then combined lines have no duplicates and the newest line wins on conflict.
- Given both carts have the same product, then the line quantity is the newer `UpdatedAt` value.

**US-C-004 — Live Totals**
- Given any cart mutation, then item subtotal, discounts, tax, shipping estimate, and total are recomputed in the active currency (4-dp math, 2-dp display).
- Given quantity set to 0, then the line is removed. Given quantity 100, then 422.

---

## 6. Epic D — Checkout & Orders

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-D-001 | As **Sarah**, I want guest checkout, so that I can buy without an account | Must | L | BR-1401, FRS-D-001 |
| US-D-002 | As **Sarah**, I want live shipping rates at checkout, so that I choose informed | Must | M | BR-1402, FRS-H |
| US-D-003 | As **Ahmed**, I want stock validated and reserved at placement, so that I never get oversold | Must | L | BR-1403, FRS-D-002/UC-F-003 |
| US-D-004 | As **Ahmed**, I want a unique order number, so that I can reference it | Must | S | BR-1405, FRS-D-003 |
| US-D-005 | As **Ahmed**, I want an order confirmation with full summary, so that I have proof of purchase | Must | M | BR-1406 |
| US-D-006 | As **Ahmed**, I want order history and detail with a timeline, so that I can track my purchases | Must | M | BR-1407, FRS-D-005 |
| US-D-007 | As **Ahmed**, I want to cancel when allowed, so that I can change my mind safely | Must | M | BR-1408, FRS-D-006 |
| US-D-008 | As **Ahmed**, I want to reorder, so that repeat purchases take seconds | Should | M | BR-1409, FRS-D-007 |
| US-D-009 | As **Diego**, I want to look up orders by number/email, so that I can resolve issues | Must | S | BR-1412, FRS-D-008 |

**US-D-001 — Guest Checkout**
- Given I am a guest with a non-empty cart, when I initiate checkout, then I can complete payment and receive an order tied to my email.
- Given I later register with that email, then the order is linked to my account.
- Given my cart is empty at placement, then 409 and no order.

**US-D-003 — Reservation at Placement (Atomic)**
- Given I submit a valid checkout, when I place the order, then payment authorization, stock allocation, and order persistence happen in one transaction.
- Given stock ran out meanwhile, then 409 `ERR_STK_001` and nothing persists.
- Given I resubmit with the same idempotency key, then I get the original order (no duplicate).

**US-D-007 — Cancel**
- Given my order is Paid and not yet picking, when I cancel, then it becomes Cancelled, stock is released, and refund is queued.
- Given my order is already Shipped, then cancellation is rejected with 409 and a return path is offered.

---

## 7. Epic E — Pricing, Discounts & Promotions

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-E-001 | As **Elena**, I want discount types (product/order/shipping), so that campaigns are flexible | Must | M | BR-1701, FRS-E-001 |
| US-E-002 | As **Elena**, I want promotion campaigns with conditions, so that I can target precisely | Must | L | BR-1702, FRS-E-002 |
| US-E-003 | As **Elena**, I want coupon codes with limits, so that I can cap redemption | Must | M | BR-1703, FRS-E-003 |
| US-E-004 | As **Elena**, I want stacking control, so that discounts don't collide | Must | M | BR-1704, FRS-E-004 |
| US-E-005 | As **Sarah**, I want totals that can never go negative, so that pricing is always sane | Must | S | BR-1705, FRS-E-005 |
| US-E-006 | As **Elena**, I want country/currency eligibility, so that campaigns respect markets | Must | M | BR-1706, FRS-E-006 |
| US-E-007 | As **Elena**, I want scheduled campaigns with pause, so that launches are controlled | Should | M | BR-1709, FRS-E-008 |
| US-E-008 | As **Ahmed**, I want my order's discount snapshot kept, so that history never changes | Must | M | BR-1710, FRS-E-009 |

**US-E-003 — Coupon Redemption (Atomic)**
- Given a coupon with 100 total redemptions, when 2,000 users race to redeem, then exactly 100 succeed.
- Given I already used a per-customer coupon, then it's rejected with `COUPON_ALREADY_USED`.
- Given my order is cancelled, then the redemption counter is rolled back.

**US-E-004 — Stacking**
- Given a campaign marked non-stackable and a cart coupon, then only one applies (highest value per priority rule).
- Given stackable campaigns, then evaluation order is item → cart → shipping.

---

## 8. Epic F — Inventory & Warehouses

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-F-001 | As **Elena**, I want warehouse management, so that stock is organized per location | Must | M | BR-1801/1802, FRS-F-001 |
| US-F-002 | As **Marcus**, I want a stock ledger, so that every movement is traceable | Must | M | BR-1805, FRS-F-002 |
| US-F-003 | As **Ahmed**, I want atomic stock allocation, so that I'm never oversold | Must | L | BR-1803/1804, FRS-F-003 |
| US-F-004 | As **Marcus**, I want stock adjustments with reason and approval, so that counts stay accurate | Must | M | BR-1807, FRS-F-005 |
| US-F-005 | As **Elena**, I want warehouse transfers, so that stock flows where needed | Must | M | BR-1808, FRS-F-006 |
| US-F-006 | As **Marcus**, I want low-stock alerts, so that I replenish before stock-outs | Must | S | BR-1806, FRS-F-007 |
| US-F-007 | As **Ahmed**, I want backorders tracked and notified, so that I know when stock arrives | Should | M | BR-1809, FRS-F-008 |
| US-F-008 | As **Elena**, I want availability projections, so that planning is data-driven | Should | S | BR-1810, FRS-F-009 |

**US-F-003 — Atomic Allocation**
- Given 1,000 concurrent orders for the last 10 units, when allocation runs, then exactly 10 succeed and the invariant `allocated ≤ on_hand` holds at all times.
- Given allocation fails, then the checkout fails cleanly with `ERR_STK_001` and partial reservations are released.

**US-F-004 — Adjustments**
- Given a positive adjustment, then ledger records in with reason.
- Given a negative adjustment without approval, then it is rejected 422.
- Given an approved negative adjustment, then ledger records the out with approver id.

---

## 9. Epic G — Payments

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-G-001 | As **Ahmed**, I want multiple payment providers, so that I can pay with my preferred method | Must | L | BR-1901, FRS-G-001 |
| US-G-002 | As **Ahmed**, I want authorization at checkout and capture at fulfillment, so that I'm charged correctly | Must | L | BR-1903, FRS-G-002 |
| US-G-003 | As **Ingrid**, I want provider failover, so that checkout survives a PSP outage | Should | L | BR-1902, FRS-G-004 |
| US-G-004 | As **Sarah**, I want declined payments retried safely, so that I can complete my order | Must | M | BR-1905, FRS-G-005 |
| US-G-005 | As **Ingrid**, I want webhooks processed idempotently, so that duplicates don't double-charge | Must | L | BR-1904, FRS-G-003 |
| US-G-006 | As **Ingrid**, I want no raw card data stored, so that PCI scope stays out | Must | M | BR-1906, FRS-G-006 |
| US-G-007 | As **Priya**, I want a payment ledger, so that reconciliation is possible | Must | M | BR-1907, FRS-G-007 |
| US-G-008 | As **Priya**, I want nightly reconciliation, so that drift is caught automatically | Should | M | BR-1909, FRS-G-009 |

**US-G-005 — Webhook Idempotency**
- Given a PSP delivers the same event twice, then the effect happens exactly once.
- Given an event with an older timestamp than the current state, then it is rejected as stale.
- Given an event with an invalid signature, then it is rejected 401 and logged.

**US-G-002 — Authorize & Capture**
- Given checkout with a valid payment method, then an authorization is created and amount reserved.
- Given order ready for fulfillment, then capture executes with amount ≤ authorization.
- Given authorization expired before capture, then it is voided and the order returns to AwaitingPayment.

---

## 10. Epic H — Shipping & Fulfillment

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-H-001 | As **Marcus**, I want live fulfillment queues, so that I work the newest orders first | Must | M | BR-1501, FRS-H-003 |
| US-H-002 | As **Marcus**, I want zone-grouped pick lists, so that picking is efficient | Must | M | BR-1502, FRS-H-004 |
| US-H-003 | As **Marcus**, I want packed orders to generate labels and tracking, so that shipping is one step | Must | M | BR-1504, FRS-H-005 |
| US-H-004 | As **Ahmed**, I want tracking updates, so that I know where my order is | Must | M | BR-1505 |
| US-H-005 | As **Ahmed**, I want split shipments, so that each warehouse's part is tracked | Should | L | BR-1506, FRS-H-006 |
| US-H-006 | As **Diego**, I want to correct an address before shipment, so that deliveries succeed | Should | S | BR-1507, FRS-H-007 |
| US-H-007 | As **Ahmed**, I want delivery confirmation, so that my order closes correctly | Must | S | BR-1509, FRS-H-008 |

**US-H-001 — Fulfillment Queue**
- Given a paid order assigned to my warehouse, when payment confirms, then the task appears in my queue within 5 s (SignalR push + REST fallback).
- Given I process a task, then its status transitions Picking → Packed and is visible to the customer timeline.

**US-H-004 — Tracking**
- Given a shipment exists, when a carrier status event arrives, then the order state and customer notifications update accordingly.
- Given carrier webhook delivery fails, then polling retries and stale events are ignored.

---

## 11. Epic I — Finance & Refunds

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-I-001 | As **Priya**, I want invoices generated on payment, so that records are complete | Must | M | BR-?/FRS-I-001 |
| US-I-002 | As **Priya**, I want credit notes tied to refunds, so that the ledger balances | Must | M | BR-1608, FRS-I-002 |
| US-I-003 | As **Priya**, I want per-country tax calculation, so that compliance holds | Must | M | BR-?/FRS-I-003 |
| US-I-004 | As **Priya**, I want a policy-controlled refund workflow, so that no refund slips through | Must | L | BR-1608, FRS-I-004 |
| US-I-005 | As **Priya**, I want a reconciliation feed, so that GL uploads are easy | Must | M | BR-2206, FRS-I-005 |
| US-I-006 | As **Priya**, I want refunds idempotent and never duplicated, so that money is safe | Must | L | BR-1607, FRS-I-004 |
| US-I-007 | As **Priya**, I want financial audit trail, so that every cent is explainable | Must | M | BR-?/FRS-I-006 |

**US-I-004 — Refund Workflow**
- Given a refund request within policy, when approved, then it executes idempotently via PSP and produces a credit note.
- Given a refund greater than the remaining refundable amount, then it's rejected 409 `ERR_PAY_003`.
- Given PSP failure, then refund is Failed and retried (max 5, backoff); customer notified at terminal states.
- Given duplicate execution attempts, then only one refund occurs.

---

## 12. Epic J — Notifications

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-J-001 | As **Ahmed**, I want lifecycle notifications, so that I stay informed | Must | M | BR-2001, FRS-J-001 |
| US-J-002 | As **Ahmed**, I want email and SMS channels, so that I get updates my way | Must | M | BR-2002, FRS-J-002 |
| US-J-003 | As **Ahmed**, I want preference control, so that I'm not spammed | Must | S | BR-2003, FRS-J-003 |
| US-J-004 | As **Elena**, I want localized templates, so that messages fit each market | Must | M | BR-2004, FRS-J-004 |
| US-J-005 | As **Ingrid**, I want reliable delivery with retries, so that messages aren't lost | Must | M | BR-2005, FRS-J-005 |
| US-J-006 | As **Ingrid**, I want PII-safe payloads, so that privacy holds | Must | S | BR-2006, FRS-J-006 |

**US-J-001 — Lifecycle Notifications**
- Given an order is placed, shipped, delivered, or cancelled, then the matching notification is queued via events.
- Given a gateway failure, then the job retries with backoff and alerts after the limit.
- Given duplicate events, then no duplicate notification is sent (dedupe key = event+channel+recipient).

---

## 13. Epic K — Reviews & Ratings

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-K-001 | As **Ahmed**, I want to review verified purchases, so that my feedback is credible | Must | M | BR-2101, FRS-K-001 |
| US-K-002 | As **Diego**, I want moderation before publication, so that abuse is filtered | Must | M | BR-2102, FRS-K-002 |
| US-K-003 | As **Ahmed**, I want accurate product ratings, so that I trust the aggregate | Must | M | BR-2103, FRS-K-003 |
| US-K-004 | As **Diego**, I want to remove abusive reviews, so that the community stays safe | Must | S | BR-2104, FRS-K-004 |
| US-K-005 | As **Ahmed**, I want to vote reviews helpful/not, so that quality surfaces | Could | S | BR-2106, FRS-K-005 |

**US-K-001 — Review Submission**
- Given I purchased the product, when I submit a 1–5 review, then it's queued for moderation with a Verified Purchase flag.
- Given I did not purchase, then 403.
- Given I already reviewed this product, then 409 `ERR_RES_003`.

**US-K-003 — Rating Aggregation**
- Given a review is published, then the product rating recomputes immediately and cached values invalidate.
- Given a review is removed, then the rating recomputes and never drifts.

---

## 14. Epic L — Analytics & Reporting

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-L-001 | As **Priya**, I want sales analytics, so that I track revenue and orders | Must | M | BR-2201, FRS-L-001 |
| US-L-002 | As **Elena**, I want product performance, so that I decide assortment | Must | M | BR-2202, FRS-L-002 |
| US-L-003 | As **Priya**, I want inventory reports, so that I manage capital | Should | M | BR-2203, FRS-L-003 |
| US-L-004 | As **Elena**, I want promotion performance, so that I prove ROI | Should | M | BR-2204, FRS-L-004 |
| US-L-005 | As **Marcus**, I want fulfillment metrics, so that ops improve | Should | M | BR-2205, FRS-L-005 |
| US-L-006 | As **Priya**, I want finance reports, so that GL reconciliation is fast | Must | M | BR-2206, FRS-L-006 |
| US-L-007 | As **Priya**, I want async exports, so that big reports don't block | Must | M | BR-2207, FRS-L-007 |

**US-L-007 — Async Export**
- Given I request a report over 500k rows, then it runs as a background job and returns a download link (TTL 24 h).
- Given export fails, then a retry job runs and I'm notified.

---

## 15. Epic M — Platform & Governance

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-M-001 | As **Ingrid**, I want an audit log, so that I can prove actions | Must | L | BR-2301, FRS-M-001 |
| US-M-002 | As **Ingrid**, I want feature flags with kill-switch, so that risky changes roll back fast | Must | M | BR-2302, FRS-M-002 |
| US-M-003 | As **Ingrid**, I want durable background jobs, so that async work is reliable | Must | M | BR-2303, FRS-M-003 |
| US-M-004 | As **Yusuf**, I want signed webhooks with replay, so that integrations are dependable | Should | L | BR-2304, FRS-M-004 |
| US-M-005 | As **Yusuf**, I want rate limiting with clear headers, so that I integrate safely | Must | S | BR-2305, FRS-M-005 |
| US-M-006 | As **Ingrid**, I want health endpoints, so that SRE can react | Must | S | BR-2307, FRS-M-006 |
| US-M-007 | As **Yusuf**, I want versioned APIs with deprecation policy, so that my clients don't break | Must | S | BR-2305, FRS-M-007 |
| US-M-008 | As **Elena**, I want bulk operations with error reports, so that large tasks are safe | Should | M | BR-2308, FRS-M-008 |

**US-M-001 — Audit Log**
- Given any protected operation, then a tamper-evident audit record (who/what/when/from/to + hash chain) is written.
- Given an auditor query, then results are searchable by actor, action, entity, and time range.
- Given tampering, then hash-chain verification detects it.

**US-M-004 — Webhooks**
- Given a subscribed event occurs, then a signed HMAC payload is delivered with retries on failure.
- Given a missed event, then the partner can replay from `after=eventId`.
- Given a failing endpoint, then delivery stops escalating and operators are alerted.

---

## 16. Epic N — Real-time

| ID | Story | Pri | Size | Ref |
|----|-------|:---:|:----:|-----|
| US-N-001 | As **Ahmed**, I want order status pushed live, so that I don't refresh | Must | M | BR-2001, FRS-N-001 |
| US-N-002 | As **Marcus**, I want new tasks pushed to my queue, so that I react instantly | Should | M | BR-1501, FRS-N-002 |
| US-N-003 | As **Elena**, I want live operational tiles, so that I monitor the store | Should | S | BR-2208, FRS-N-003 |
| US-N-004 | As **Ingrid**, I want missed events recovered on reconnect, so that nothing is lost | Must | M | BR-?/FRS-N-004 |

**US-N-001 — Order Push**
- Given I'm connected to the order hub, when my order state changes, then I receive the update within 5 s.
- Given I disconnect and reconnect, then I resume from my last `eventId` and missed events replay.

---

## 17. Definition of Ready / Done

### 17.1 Definition of Ready
- Story has acceptance criteria (Given/When/Then or checklist) and a traceable FRS ID.
- Dependencies and impacted slices identified.
- Persona/actor named; permissions mapped.
- Sized; no ambiguous terms; edge cases noted.

### 17.2 Definition of Done (applies to every story above)
- FRS behavior implemented; validation + error codes match `FRS §3`.
- Domain events/outbox and audit entries fire where FRS requires.
- Unit + integration tests pass; architecture tests green.
- Swagger/OpenAPI updated; metrics/logging/traces present.
- Performance targets in `05` respected (checked for hot paths).

---

## 18. Backlog Traceability (Summary)

| Source | Count |
|--------|------:|
| BRD requirements mapped to stories | 194 → 103 stories (1.x avg) |
| FRS requirements anchored | ~120 |
| Stories with full Gherkin scenarios | 28 |
| Stories with condensed AC checklist | 75 |

---

## 19. Approvals

| Role         | Name | Decision | Date |
|--------------|------|----------|------|
| Product Owner | —    | —        | —    |
| Technical Lead | —   | —        | —    |
| QA Lead       | —    | —        | —    |

---

*End of Document 03a — User Stories with Acceptance Criteria.*
*Next document on request: `02-glossary-and-definitions.md`.*
