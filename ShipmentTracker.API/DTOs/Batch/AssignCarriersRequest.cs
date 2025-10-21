using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Batch;

public class AssignCarriersRequest
{
    [Required]
    public List<ShipmentCarrierAssignment> Assignments { get; set; } = new();
}

public class ShipmentCarrierAssignment
{
    [Required]
    public long ShipmentId { get; set; }

    [Required]
    public long CarrierId { get; set; }
}
