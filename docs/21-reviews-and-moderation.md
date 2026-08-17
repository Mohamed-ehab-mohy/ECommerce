# 21 — Reviews & Moderation

## 1. Overview

Customers can submit reviews (with rating + comment) for products they have purchased. Reviews enter a moderation queue and are published or rejected by moderators. Published reviews support helpful-vote aggregation. The rating aggregate is recomputed on publish/remove (FRS-K-003).

## 2. Domain Entities

| Entity | Namespace | Purpose |
|--------|-----------|---------|
| `ProductReview` | `Reviews` | Customer review aggregate. Owns status lifecycle and moderation metadata. |
| `ReviewVote` | `Reviews` | One customer's helpful/not-helpful vote on a published review (FRS-K-005). |
| `ReviewVoteValue` (enum) | `Reviews` | `Helpful` / `NotHelpful`. |
| `ProductReviewStatus` (enum) | `Reviews` | `Pending → Published | Rejected`; `Published → Removed`. |

### Review Lifecycle

```
ProductReview.Create()  ──→  ProductReviewStatus.Pending
         │
    Publish()             ──→  ProductReviewStatus.Published  ──→  Remove()  ──→  ProductReviewStatus.Removed
    Reject()              ──→  ProductReviewStatus.Rejected
```

- Submitting a review emits `ReviewSubmitted` domain event.
- Publishing emits `ReviewPublished` (triggers rating aggregate recalculation + search index sync).
- Rejecting emits `ReviewRejected`.
- Removing emits `ReviewRemoved`.
- One review per customer per product is enforced via `ExistsAsync()` in the repository.

### Vote Behavior

A customer may vote once per review. Repeating a vote changes its value (upsert semantics) via `ReviewVote.Change()`. Votes are only allowed on `Published` reviews.

## 3. Key Operations

| Operation | Trigger | Handler |
|-----------|---------|---------|
| Submit review | Customer API call | `SubmitReviewCommand` — creates `ProductReview` with `Pending` status, checks verified purchase |
| Vote on review | Customer API call | `VoteReviewCommand` — upserts `ReviewVote` on a published review |
| List published reviews | Public API call | `ListProductReviewsQuery` — paginated, includes aggregate rating |
| List moderation queue | Admin API call | `GetModerationQueueQuery` — returns all `Pending` reviews |
| Publish review | Moderator API call | `PublishReviewCommand` — transitions `Pending → Published` |
| Reject review | Moderator API call | `RejectReviewCommand` — transitions `Pending → Rejected` with reason |
| Remove review | Moderator API call | `RemoveReviewCommand` — transitions `Published → Removed` with reason |

### Verified Purchase Check

`VerifiedPurchaseChecker` queries `Order.Items` to verify the customer has a non-cancelled order containing the product. This is checked at submission time.

## 4. API Endpoints

| Method | Route | Controller | Auth | Description |
|--------|-------|------------|------|-------------|
| `GET` | `/api/v1/products/{productId}/reviews` | `ReviewsController` | Public | List published reviews for a product |
| `POST` | `/api/v1/products/{productId}/reviews` | `ReviewsController` | Customer | Submit a review (202 Accepted) |
| `POST` | `/api/v1/reviews/{reviewId}/vote` | `ReviewsController` | Customer | Vote on a review |
| `GET` | `/api/v1/reviews/moderate` | `ReviewsController` | Moderator | Get moderation queue |
| `POST` | `/api/v1/reviews/{reviewId}/publish` | `ReviewsController` | Moderator | Publish a pending review |
| `POST` | `/api/v1/reviews/{reviewId}/reject` | `ReviewsController` | Moderator | Reject a pending review |
| `POST` | `/api/v1/reviews/{reviewId}/remove` | `ReviewsController` | Moderator | Remove a published review |

## 5. Integration Points

- **Domain events → Outbox**: `ReviewSubmitted`, `ReviewPublished`, `ReviewRejected`, `ReviewRemoved` published via outbox pattern.
- **Search sync**: `ReviewRatingSynchronizer` listens for review events and updates the product search index rating aggregate.
- **Audit logging**: Review moderation actions are recorded via `AuditActions.ReviewSubmitted`, `ReviewModerated`, `ReviewRemovedAction`.

## 6. File References

| File | Path |
|------|------|
| `ProductReview.cs` | `src/ECommerce.Domain/Reviews/ProductReview.cs` |
| `ReviewVote.cs` | `src/ECommerce.Domain/Reviews/ReviewVote.cs` |
| `ProductReviewStatus.cs` | `src/ECommerce.Domain/Reviews/ProductReviewStatus.cs` |
| `ProductReviewErrors.cs` | `src/ECommerce.Domain/Reviews/ProductReviewErrors.cs` |
| `ReviewVoteValue.cs` | `src/ECommerce.Domain/Reviews/ReviewVoteValue.cs` |
| `ReviewsController.cs` | `src/ECommerce.API/Controllers/ReviewsController.cs` |
| `ProductReviewRepository.cs` | `src/ECommerce.Infrastructure/Reviews/ProductReviewRepository.cs` |
| `ReviewVoteRepository.cs` | `src/ECommerce.Infrastructure/Reviews/ReviewVoteRepository.cs` |
| `VerifiedPurchaseChecker.cs` | `src/ECommerce.Infrastructure/Reviews/VerifiedPurchaseChecker.cs` |
| `ReviewRatingSynchronizer.cs` | `src/ECommerce.Infrastructure/Search/ReviewRatingSynchronizer.cs` |
