using System.ComponentModel.DataAnnotations;
using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.API.DTOs.Shipment;

public class UpdateShipmentStatusRequest
{
    [Required]
    public ShipmentStatus Status { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
