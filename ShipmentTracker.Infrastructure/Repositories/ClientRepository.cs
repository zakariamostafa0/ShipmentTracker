using Microsoft.EntityFrameworkCore;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;
using ShipmentTracker.Infrastructure.Data;

namespace ShipmentTracker.Infrastructure.Repositories;

public class ClientRepository : GenericRepository<Client>, IClientRepository
{
    public ClientRepository(ShipmentTrackerDbContext context) : base(context)
    {
    }

    public async Task<Client?> GetClientByUserIdAsync(long userId)
    {
        return await _context.Clients
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<IEnumerable<Client>> GetTopClientsByBranchAsync(long branchId, int count = 10)
    {
        return await _context.Clients
            .Include(c => c.User)
            .Include(c => c.Shipments.Where(s => s.Batch != null && s.Batch.BranchId == branchId))
            .Where(c => c.Shipments.Any(s => s.Batch != null && s.Batch.BranchId == branchId))
            .OrderByDescending(c => c.Shipments.Count(s => s.Batch != null && s.Batch.BranchId == branchId))
            .Take(count)
            .ToListAsync();
    }

    public async Task<Client?> GetClientWithShipmentsAsync(long clientId)
    {
        return await _context.Clients
            .Include(c => c.User)
            .Include(c => c.Shipments)
                .ThenInclude(s => s.Batch)
            .Include(c => c.Shipments)
                .ThenInclude(s => s.Carrier)
            .Include(c => c.Shipments)
                .ThenInclude(s => s.Events)
            .FirstOrDefaultAsync(c => c.Id == clientId);
    }

    public async Task<IEnumerable<Client>> GetClientsByBranchAsync(long branchId)
    {
        return await _context.Clients
            .Include(c => c.User)
            .Include(c => c.Shipments.Where(s => s.Batch != null && s.Batch.BranchId == branchId))
            .Where(c => c.Shipments.Any(s => s.Batch != null && s.Batch.BranchId == branchId))
            .ToListAsync();
    }
}
