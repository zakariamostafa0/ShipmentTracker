namespace ShipmentTracker.Core.Entities;

public class Port : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Batch> SourceBatches { get; set; } = new List<Batch>();
    public virtual ICollection<Batch> DestinationBatches { get; set; } = new List<Batch>();
}
