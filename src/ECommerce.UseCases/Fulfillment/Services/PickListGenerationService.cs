using ECommerce.Domain.Fulfillment;
using ECommerce.UseCases.Fulfillment.Responses;

namespace ECommerce.UseCases.Fulfillment.Services;

public sealed class PickListGenerationService
{
    public const int DefaultMaxLinesPerList = 25;

    public IReadOnlyList<PickListResponse> Generate(
        string warehouseCode,
        IReadOnlyList<FulfillmentTask> tasks,
        IReadOnlyDictionary<Guid, string> orderNumberByOrderId,
        int maxLinesPerList = DefaultMaxLinesPerList)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLinesPerList, 1);

        var lines = new List<PickLine>();

        foreach (var task in tasks)
        {
            if (!orderNumberByOrderId.TryGetValue(task.OrderId, out var orderNumber))
            {
                continue;
            }

            var zone = task.Zone ?? "UNZONED";
            foreach (var item in task.Items)
            {
                lines.Add(new PickLine(zone, orderNumber, task.Id, item.Sku, item.BinLocation, item.Quantity));
            }
        }

        var lists = new List<PickListResponse>();

        foreach (var zoneGroup in lines
            .GroupBy(line => line.Zone)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var ordered = zoneGroup
                .OrderBy(line => line.BinLocation is null ? 1 : 0)
                .ThenBy(line => line.BinLocation, StringComparer.Ordinal)
                .ThenBy(line => line.Sku, StringComparer.Ordinal)
                .ThenBy(line => line.TaskId)
                .ToList();

            for (var index = 0; index < ordered.Count; index += maxLinesPerList)
            {
                var chunk = ordered.Skip(index).Take(maxLinesPerList).ToList();
                lists.Add(new PickListResponse(
                    zoneGroup.Key,
                    warehouseCode,
                    chunk.Count,
                    chunk.Sum(line => line.Quantity),
                    chunk
                        .Select(line => new PickListLineResponse(
                            line.TaskId,
                            line.OrderNumber,
                            line.Sku,
                            line.BinLocation,
                            line.Quantity))
                        .ToList()));
            }
        }

        return lists;
    }

    private sealed record PickLine(
        string Zone,
        string OrderNumber,
        Guid TaskId,
        string Sku,
        string? BinLocation,
        int Quantity);
}
