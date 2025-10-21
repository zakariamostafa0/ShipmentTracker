using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Batch;

public class UpdateBatchStatusRequest
{
    [StringLength(500)]
    public string? Notes { get; set; }
}
