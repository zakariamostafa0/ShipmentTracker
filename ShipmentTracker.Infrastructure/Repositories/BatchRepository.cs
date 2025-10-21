using Microsoft.EntityFrameworkCore;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;
using ShipmentTracker.Core.Interfaces;
using ShipmentTracker.Infrastructure.Data;

namespace ShipmentTracker.Infrastructure.Repositories;

public class BatchRepository : GenericRepository<Batch>, IBatchRepository
{
    public BatchRepository(ShipmentTrackerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Batch>> GetBatchesByBranchAsync(long branchId)
    {
        return await _dbSet
            .Where(b => b.BranchId == branchId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Batch>> GetBatchesByStatusAsync(BatchStatus status)
    {
        return await _dbSet
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Batch>> GetBatchesByBranchAndStatusAsync(long branchId, BatchStatus status)
    {
        return await _dbSet
            .Where(b => b.BranchId == branchId && b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Batch?> GetWithShipmentsAsync(long batchId)
    {
        return await _dbSet
            .Include(b => b.Shipments)
            .Include(b => b.Branch)
            .Include(b => b.SourceWarehouse)
            .Include(b => b.DestinationWarehouse)
            .Include(b => b.SourcePort)
            .Include(b => b.DestinationPort)
            .FirstOrDefaultAsync(b => b.Id == batchId);
    }

    public async Task<IEnumerable<Batch>> GetOpenBatchesAsync()
    {
        return await _dbSet
            .Where(b => b.Status == BatchStatus.Draft || b.Status == BatchStatus.Open)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}
