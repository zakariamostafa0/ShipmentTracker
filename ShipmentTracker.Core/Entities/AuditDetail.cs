namespace ShipmentTracker.Core.Entities;

public class AuditDetail : BaseEntity
{
    public long AuditHeaderId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    
    // Navigation properties
    public virtual AuditHeader AuditHeader { get; set; } = null!;
}
