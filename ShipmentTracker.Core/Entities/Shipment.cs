using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.Core.Entities;

public class Shipment : BaseEntity
{
    public long ClientId { get; set; }
    public long? BatchId { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
    public decimal Weight { get; set; }
    public decimal? Volume { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public long? CarrierId { get; set; }
    
    // Navigation properties
    public virtual Client Client { get; set; } = null!;
    public virtual Batch? Batch { get; set; }
    public virtual Carrier? Carrier { get; set; }
    public virtual ICollection<ShipmentEvent> Events { get; set; } = new List<ShipmentEvent>();
}
