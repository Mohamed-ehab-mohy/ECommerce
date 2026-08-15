using ECommerce.Domain.Reviews;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Catalog.Ports;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Ports;
using ECommerce.UseCases.Reviews.Queries;
using ECommerce.UseCases.Reviews.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Reviews.Handlers;

/// <summary>
/// Lists published reviews for an active product and returns the aggregated rating across all of
/// them (US-K-003). Reviews of deactivated products are retained in storage but hidden here.
/// </summary>
public sealed class ListProductReviewsQueryHandler(
    IProductRepository products,
    IProductReviewRepository reviews,
    IReviewVoteRepository votes,
    IValidator<ListProductReviewsQuery> validator) : IRequestHandler<ListProductReviewsQuery, Result<ProductReviewsResponse>>
{
    public async Task<Result<ProductReviewsResponse>> Handle(
        ListProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<ProductReviewsResponse>();
        }

        var product = await products.GetActiveByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductReviewErrors.ProductNotFound;
        }

        var published = await reviews.ListPublishedByProductAsync(request.ProductId, cancellationToken);

        var ratingAverage = published.Count == 0
            ? 0m
            : Math.Round((decimal)published.Average(review => review.Rating), 2, MidpointRounding.AwayFromZero);

        var items = published
            .OrderByDescending(review => review.ModeratedAt ?? review.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responseItems = new List<ProductReviewResponse>(items.Count);
        foreach (var review in items)
        {
            var helpfulVotes = await votes.CountHelpfulAsync(review.Id, cancellationToken);
            responseItems.Add(new ProductReviewResponse(
                review.Id,
                review.ProductId,
                review.Rating,
                review.Comment,
                review.VerifiedPurchase,
                review.ModeratedAt,
                helpfulVotes));
        }

        return new ProductReviewsResponse(
            request.ProductId,
            ratingAverage,
            published.Count,
            request.Page,
            request.PageSize,
            published.Count,
            responseItems);
    }
}
