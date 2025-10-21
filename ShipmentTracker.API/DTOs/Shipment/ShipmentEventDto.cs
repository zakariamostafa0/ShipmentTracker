namespace ShipmentTracker.API.DTOs.Shipment;

public class ShipmentEventDto
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long? ActorUserId { get; set; }
    public string ActorUserName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
