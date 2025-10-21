using ShipmentTracker.API.DTOs.Shipment;

namespace ShipmentTracker.API.DTOs.Batch;

public class BatchDetailResponse : BatchResponse
{
    public List<ShipmentResponse> Shipments { get; set; } = new();
}
