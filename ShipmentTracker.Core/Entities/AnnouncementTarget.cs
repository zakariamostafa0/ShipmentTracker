namespace ShipmentTracker.Core.Entities;

public class AnnouncementTarget : BaseEntity
{
    public long AnnouncementId { get; set; }
    public long? BranchId { get; set; }
    public long? ClientId { get; set; }
    public string? Tag { get; set; }
    
    // Navigation properties
    public virtual Announcement Announcement { get; set; } = null!;
    public virtual Branch? Branch { get; set; }
    public virtual Client? Client { get; set; }
}
