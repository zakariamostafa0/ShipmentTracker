using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.API.DTOs.Shipment;

public class ShipmentResponse
{
    public long Id { get; set; }
    public long ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public long? BatchId { get; set; }
    public string? BatchName { get; set; }
    public ShipmentStatus Status { get; set; }
    public decimal Weight { get; set; }
    public decimal? Volume { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public long? CarrierId { get; set; }
    public string? CarrierName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
