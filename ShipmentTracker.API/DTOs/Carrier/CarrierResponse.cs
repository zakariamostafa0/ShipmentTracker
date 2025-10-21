namespace ShipmentTracker.API.DTOs.Carrier;

public class CarrierResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactInfo { get; set; } = string.Empty;
    public int ActiveShipmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
