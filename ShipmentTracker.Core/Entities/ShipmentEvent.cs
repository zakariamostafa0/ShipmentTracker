namespace ShipmentTracker.Core.Entities;

public class ShipmentEvent : BaseEntity
{
    public long ShipmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long? ActorUserId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual User? ActorUser { get; set; }
}
