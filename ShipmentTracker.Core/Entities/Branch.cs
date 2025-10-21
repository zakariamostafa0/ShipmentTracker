namespace ShipmentTracker.Core.Entities;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public virtual ICollection<AnnouncementTarget> AnnouncementTargets { get; set; } = new List<AnnouncementTarget>();
}
