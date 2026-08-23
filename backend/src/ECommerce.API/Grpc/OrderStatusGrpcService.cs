using ECommerce.UseCases.Grpc.Ports;
using Grpc.Core;

namespace ECommerce.API.Grpc;

public sealed class OrderStatusGrpcService(
    IGrpcQueryService queryService,
    ILogger<OrderStatusGrpcService> logger) : Protos.OrderStatusService.OrderStatusServiceBase
{
    public override async Task<Protos.OrderStatusResponse> GetByNumber(
        Protos.OrderNumberRequest request,
        ServerCallContext context)
    {
        var dto = await queryService.GetOrderStatusAsync(request.OrderNumber, context.CancellationToken);

        if (dto is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Order '{request.OrderNumber}' not found."));
        }

        var response = new Protos.OrderStatusResponse
        {
            OrderNumber = dto.OrderNumber,
            Status = dto.Status,
            PaymentStatus = dto.PaymentStatus,
            CustomerEmail = dto.CustomerEmail
        };

        if (dto.PlacedAt.HasValue)
        {
            response.PlacedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.PlacedAt.Value);
        }

        foreach (var log in dto.Timeline)
        {
            var entry = new Protos.OrderTimelineEntry
            {
                Status = log.Status,
                Note = log.Note
            };

            entry.OccurredAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(log.OccurredAt);
            response.Timeline.Add(entry);
        }

        return response;
    }

    public override async Task Subscribe(
        Protos.SubscribeRequest request,
        IServerStreamWriter<Protos.OrderStatusEvent> responseStream,
        ServerCallContext context)
    {
        logger.LogInformation("gRPC Subscribe called for order {OrderNumber} (placeholder stream)", request.OrderNumber);

        await Task.Delay(Timeout.Infinite, context.CancellationToken);
    }
}
