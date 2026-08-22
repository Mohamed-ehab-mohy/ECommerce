using ECommerce.API.Common;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Commands;
using ECommerce.UseCases.Reviews.Queries;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ReviewsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Lists published reviews for a product with the aggregated rating (public).</summary>
    [AllowAnonymous]
    [HttpGet("products/{productId:guid}/reviews")]
    public async Task<IActionResult> List(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ListProductReviewsQuery(productId, page, pageSize),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Submits a review for a verified purchase; queued for moderation.</summary>
    [Authorize]
    [HttpPost("products/{productId:guid}/reviews")]
    public async Task<IActionResult> Submit(
        Guid productId,
        SubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SubmitReviewCommand(productId, currentUser.UserId ?? Guid.Empty, request.Rating, request.Comment),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : StatusCode(StatusCodes.Status202Accepted, result.Value);
    }

    /// <summary>Records or changes the caller's helpful vote on a published review.</summary>
    [Authorize]
    [HttpPost("reviews/{reviewId:guid}/vote")]
    public async Task<IActionResult> Vote(
        Guid reviewId,
        VoteReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new VoteReviewCommand(reviewId, currentUser.UserId ?? Guid.Empty, request.Helpful),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Returns the moderation queue of pending reviews (reviews.moderate).</summary>
    [Authorize]
    [HttpGet("reviews/moderate")]
    public async Task<IActionResult> ModerationQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetModerationQueueQuery(page, pageSize),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Approves a pending review for publication (reviews.moderate).</summary>
    [Authorize]
    [HttpPost("reviews/{reviewId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid reviewId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PublishReviewCommand(reviewId, currentUser.UserId),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Rejects a pending review with a reason (reviews.moderate).</summary>
    [Authorize]
    [HttpPost("reviews/{reviewId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid reviewId,
        ModerationReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RejectReviewCommand(reviewId, currentUser.UserId, request.Reason),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }

    /// <summary>Removes a published review for compliance/abuse (reviews.moderate).</summary>
    [Authorize]
    [HttpPost("reviews/{reviewId:guid}/remove")]
    public async Task<IActionResult> Remove(
        Guid reviewId,
        ModerationReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveReviewCommand(reviewId, currentUser.UserId, request.Reason),
            cancellationToken);

        return result.IsFailure
            ? ProblemResponse.Create(result.ToOperationError())
            : Ok(result.Value);
    }
}

public sealed record SubmitReviewRequest(int Rating, string Comment);

public sealed record VoteReviewRequest(bool Helpful);

public sealed record ModerationReviewRequest(string Reason);
