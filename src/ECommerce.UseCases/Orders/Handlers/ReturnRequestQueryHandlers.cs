using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class GetReturnRequestHandler(IReturnRequestRepository returnRequests)
    : IRequestHandler<GetReturnRequestQuery, Result<ReturnRequestResponse>>
{
    public async Task<Result<ReturnRequestResponse>> Handle(GetReturnRequestQuery request, CancellationToken cancellationToken)
    {
        var rr = await returnRequests.GetByIdAsync(request.ReturnRequestId, cancellationToken);
        return rr is null
            ? Result<ReturnRequestResponse>.Failure(new Error("ReturnRequest.NotFound", "Not found"))
            : Result<ReturnRequestResponse>.Success(Map(rr));
    }

    private static ReturnRequestResponse Map(Domain.Orders.ReturnRequest rr) =>
        new(rr.Id, rr.OrderId, rr.Reason, rr.Currency, rr.RefundAmount,
            rr.Restock, rr.Status.ToString(), rr.AdminNotes, rr.CreatedAt);
}

public sealed class ListReturnRequestsByOrderHandler(IReturnRequestRepository returnRequests)
    : IRequestHandler<ListReturnRequestsByOrderQuery, Result<IReadOnlyList<ReturnRequestResponse>>>
{
    public async Task<Result<IReadOnlyList<ReturnRequestResponse>>> Handle(ListReturnRequestsByOrderQuery request, CancellationToken cancellationToken)
    {
        var items = await returnRequests.ListByOrderAsync(request.OrderId, cancellationToken);
        return Result<IReadOnlyList<ReturnRequestResponse>>.Success(items.Select(Map).ToList());
    }

    private static ReturnRequestResponse Map(Domain.Orders.ReturnRequest rr) =>
        new(rr.Id, rr.OrderId, rr.Reason, rr.Currency, rr.RefundAmount,
            rr.Restock, rr.Status.ToString(), rr.AdminNotes, rr.CreatedAt);
}
