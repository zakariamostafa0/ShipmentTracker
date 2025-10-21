using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Shipment;

public class CreateShipmentRequest
{
    [Required]
    public long ClientId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Weight must be greater than 0")]
    public decimal Weight { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Volume must be greater than 0")]
    public decimal? Volume { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string PickupAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string DeliveryAddress { get; set; } = string.Empty;
}
