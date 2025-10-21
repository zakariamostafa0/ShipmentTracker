using Microsoft.EntityFrameworkCore;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;
using ShipmentTracker.Core.Interfaces;
using ShipmentTracker.Infrastructure.Data;

namespace ShipmentTracker.Infrastructure.Repositories;

public class ShipmentRepository : GenericRepository<Shipment>, IShipmentRepository
{
    public ShipmentRepository(ShipmentTrackerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Shipment>> GetShipmentsByClientAsync(long clientId)
    {
        return await _dbSet
            .Where(s => s.ClientId == clientId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shipment>> GetShipmentsByBatchAsync(long batchId)
    {
        return await _dbSet
            .Where(s => s.BatchId == batchId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shipment>> GetShipmentsByStatusAsync(ShipmentStatus status)
    {
        return await _dbSet
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shipment>> GetShipmentsByCarrierAsync(long carrierId)
    {
        return await _dbSet
            .Where(s => s.CarrierId == carrierId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Shipment?> GetWithEventsAsync(long shipmentId)
    {
        return await _dbSet
            .Include(s => s.Events.OrderBy(e => e.CreatedAt))
                .ThenInclude(e => e.ActorUser)
            .Include(s => s.Client)
                .ThenInclude(c => c.User)
            .Include(s => s.Batch)
            .Include(s => s.Carrier)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);
    }

    public async Task<IEnumerable<Shipment>> GetUnassignedShipmentsAsync()
    {
        return await _dbSet
            .Where(s => s.BatchId == null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}
