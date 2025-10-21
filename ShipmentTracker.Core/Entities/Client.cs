namespace ShipmentTracker.Core.Entities;

public class Client : BaseEntity
{
    public long UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
