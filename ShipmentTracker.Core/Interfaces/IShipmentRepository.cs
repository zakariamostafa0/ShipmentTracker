using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.Core.Interfaces;

public interface IShipmentRepository : IRepository<Shipment>
{
    Task<IEnumerable<Shipment>> GetShipmentsByClientAsync(long clientId);
    Task<IEnumerable<Shipment>> GetShipmentsByBatchAsync(long batchId);
    Task<IEnumerable<Shipment>> GetShipmentsByStatusAsync(ShipmentStatus status);
    Task<IEnumerable<Shipment>> GetShipmentsByCarrierAsync(long carrierId);
    Task<Shipment?> GetWithEventsAsync(long shipmentId);
    Task<IEnumerable<Shipment>> GetUnassignedShipmentsAsync();
}
