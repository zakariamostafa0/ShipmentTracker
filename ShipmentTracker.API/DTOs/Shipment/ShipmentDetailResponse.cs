namespace ShipmentTracker.API.DTOs.Shipment;

public class ShipmentDetailResponse : ShipmentResponse
{
    public List<ShipmentEventDto> Events { get; set; } = new();
}
