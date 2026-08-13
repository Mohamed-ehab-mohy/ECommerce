using ECommerce.Domain.Fulfillment;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Fulfillment.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Fulfillment;

public sealed class ShipmentRepository(ECommerceDbContext dbContext) : IShipmentRepository
{
    public Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Shipment>()
            .Include(shipment => shipment.Updates)
            .SingleOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);

    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken) =>
        dbContext.Set<Shipment>()
            .Include(shipment => shipment.Updates)
            .SingleOrDefaultAsync(shipment => shipment.TrackingNumber == trackingNumber, cancellationToken);

    public Task<bool> HasUndeliveredShipmentsAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Set<Shipment>()
            .AnyAsync(
                shipment => shipment.OrderId == orderId && shipment.Status != ShipmentStatus.Delivered,
                cancellationToken);

    public void Add(Shipment shipment) => dbContext.Set<Shipment>().Add(shipment);
}
