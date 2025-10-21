using Microsoft.EntityFrameworkCore;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;
using ShipmentTracker.Infrastructure.Data;

namespace ShipmentTracker.Infrastructure.Repositories;

public class AnnouncementRepository : GenericRepository<Announcement>, IAnnouncementRepository
{
    public AnnouncementRepository(ShipmentTrackerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Announcement>> GetActiveAnnouncementsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(a => a.StartDate <= now && a.EndDate >= now)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Announcement>> GetAnnouncementsForClientAsync(long clientId)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(a => a.Targets)
            .Where(a => a.StartDate <= now && a.EndDate >= now)
            .Where(a => a.Targets.Any(t => 
                t.ClientId == clientId || 
                t.ClientId == null)) // Global announcements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Announcement>> GetAnnouncementsForBranchAsync(long branchId)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(a => a.Targets)
            .Where(a => a.StartDate <= now && a.EndDate >= now)
            .Where(a => a.Targets.Any(t => 
                t.BranchId == branchId || 
                t.BranchId == null)) // Global announcements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Announcement?> GetWithTargetsAsync(long announcementId)
    {
        return await _dbSet
            .Include(a => a.Targets)
            .Include(a => a.CreatedByUser)
            .FirstOrDefaultAsync(a => a.Id == announcementId);
    }
}
