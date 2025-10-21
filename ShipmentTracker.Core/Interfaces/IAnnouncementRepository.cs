using ShipmentTracker.Core.Entities;

namespace ShipmentTracker.Core.Interfaces;

public interface IAnnouncementRepository : IRepository<Announcement>
{
    Task<IEnumerable<Announcement>> GetActiveAnnouncementsAsync();
    Task<IEnumerable<Announcement>> GetAnnouncementsForClientAsync(long clientId);
    Task<IEnumerable<Announcement>> GetAnnouncementsForBranchAsync(long branchId);
    Task<Announcement?> GetWithTargetsAsync(long announcementId);
}
