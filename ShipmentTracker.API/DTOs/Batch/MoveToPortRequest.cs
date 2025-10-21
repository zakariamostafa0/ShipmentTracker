using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Batch;

public class MoveToPortRequest
{
    [Required]
    public long SourcePortId { get; set; }

    public long? DestinationPortId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
