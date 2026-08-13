using ECommerce.Domain.Fulfillment;

namespace ECommerce.UseCases.Fulfillment.Ports;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken);

    Task<bool> HasUndeliveredShipmentsAsync(Guid orderId, CancellationToken cancellationToken);

    void Add(Shipment shipment);
}
