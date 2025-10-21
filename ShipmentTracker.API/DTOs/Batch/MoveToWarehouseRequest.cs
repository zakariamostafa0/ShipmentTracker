using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Batch;

public class MoveToWarehouseRequest
{
    [Required]
    public long SourceWarehouseId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class AssignDestinationWarehouseRequest
{
    [Required]
    public long DestinationWarehouseId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
