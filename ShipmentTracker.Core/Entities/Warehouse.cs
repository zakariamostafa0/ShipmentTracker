namespace ShipmentTracker.Core.Entities;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
