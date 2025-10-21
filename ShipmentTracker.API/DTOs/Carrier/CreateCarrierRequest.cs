using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Carrier;

public class CreateCarrierRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string ContactInfo { get; set; } = string.Empty;
}
