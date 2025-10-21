namespace ShipmentTracker.Core.Entities;

public class AuditHeader : BaseEntity
{
    public string TableName { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;
    public DateTime ActualDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<AuditDetail> Details { get; set; } = new List<AuditDetail>();
}
