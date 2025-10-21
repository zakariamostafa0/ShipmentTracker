using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Batch;

public class CreateBatchRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public long BranchId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Threshold count must be greater than 0")]
    public int ThresholdCount { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Threshold weight must be greater than 0")]
    public decimal ThresholdWeight { get; set; }
}
