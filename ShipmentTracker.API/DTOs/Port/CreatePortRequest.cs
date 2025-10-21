using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Port;

public class CreatePortRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Country { get; set; } = string.Empty;
}
