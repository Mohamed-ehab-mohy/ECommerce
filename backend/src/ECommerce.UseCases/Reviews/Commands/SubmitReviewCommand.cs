using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reviews.Responses;

namespace ECommerce.UseCases.Reviews.Commands;

/// <summary>Submits a review for a verified purchase; queued for moderation.</summary>
public sealed class SubmitReviewCommand(
    Guid productId,
    Guid customerId,
    int rating,
    string comment) : IRequest<Result<SubmitReviewResponse>>
{
    public Guid ProductId { get; } = productId;

    public Guid CustomerId { get; } = customerId;

    public int Rating { get; } = rating;

    public string Comment { get; } = comment;
}
