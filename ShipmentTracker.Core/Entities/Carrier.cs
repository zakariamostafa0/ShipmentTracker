namespace ShipmentTracker.Core.Entities;

public class Carrier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
