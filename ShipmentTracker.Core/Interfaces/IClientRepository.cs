using ShipmentTracker.Core.Entities;

namespace ShipmentTracker.Core.Interfaces;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetClientByUserIdAsync(long userId);
    Task<IEnumerable<Client>> GetTopClientsByBranchAsync(long branchId, int count = 10);
    Task<Client?> GetClientWithShipmentsAsync(long clientId);
    Task<IEnumerable<Client>> GetClientsByBranchAsync(long branchId);
}
