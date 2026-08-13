using ECommerce.Domain.Fulfillment;

namespace ECommerce.UseCases.Fulfillment.Ports;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken);

    void Add(Shipment shipment);
}
