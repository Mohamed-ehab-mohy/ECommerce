using ECommerce.Domain.Orders;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class RejectReturnRequestHandler(
    IReturnRequestRepository returnRequests, IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RejectReturnRequestCommand, Result>
{
    public async Task<Result> Handle(RejectReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var returnRequest = await returnRequests.GetByIdAsync(request.ReturnRequestId, cancellationToken);

        if (returnRequest is null)
            return Result.Failure(new Error("ReturnRequest.NotFound", "Return request not found"));

        if (returnRequest.Status != ReturnRequestStatus.Requested)
            return Result.Failure(new Error("ReturnRequest.InvalidStatus", "Return request is not in requested status"));

        returnRequest.Reject(Guid.Empty, request.Reason, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
