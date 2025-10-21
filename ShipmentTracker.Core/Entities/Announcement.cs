namespace ShipmentTracker.Core.Entities;

public class Announcement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long CreatedByUserId { get; set; }
    
    // Navigation properties
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<AnnouncementTarget> Targets { get; set; } = new List<AnnouncementTarget>();
}
