using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.Core.Interfaces;

public interface IBatchRepository : IRepository<Batch>
{
    Task<IEnumerable<Batch>> GetBatchesByBranchAsync(long branchId);
    Task<IEnumerable<Batch>> GetBatchesByStatusAsync(BatchStatus status);
    Task<IEnumerable<Batch>> GetBatchesByBranchAndStatusAsync(long branchId, BatchStatus status);
    Task<Batch?> GetWithShipmentsAsync(long batchId);
    Task<IEnumerable<Batch>> GetOpenBatchesAsync();
}
