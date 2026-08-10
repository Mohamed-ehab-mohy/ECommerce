using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Queries;
using ECommerce.UseCases.Orders.Responses;
using FluentValidation;
using MediatR;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class SupportOrderLookupQueryHandler(
    IOrderRepository orders,
    IValidator<SupportOrderLookupQuery> validator) : IRequestHandler<SupportOrderLookupQuery, Result<SupportOrderLookupResponse>>
{
    public async Task<Result<SupportOrderLookupResponse>> Handle(
        SupportOrderLookupQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<SupportOrderLookupResponse>();
        }

        var matched = new List<Order>();

        if (!string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            var order = await orders.GetByNumberAsync(request.OrderNumber, cancellationToken);
            if (order is not null)
            {
                matched.Add(order);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            matched.AddRange(await orders.FindByEmailAsync(request.Email, cancellationToken));
        }

        if (request.CustomerId is { } customerId)
        {
            var page = await orders.ListByCustomerAsync(customerId, null, 100, cancellationToken);
            matched.AddRange(page.Items);
        }

        var unique = matched
            .GroupBy(order => order.Id)
            .Select(group => group.First())
            .OrderByDescending(order => order.PlacedAt)
            .ToList();

        return Result<SupportOrderLookupResponse>.Success(new SupportOrderLookupResponse(
            unique
                .Select(order => new SupportOrderItemResponse(
                    order.Id,
                    order.OrderNumber,
                    order.CustomerId,
                    PiiMasker.MaskEmail(order.CustomerEmail),
                    order.Status.ToString(),
                    order.GrandTotal,
                    order.Currency,
                    order.PlacedAt))
                .ToList()));
    }
}
