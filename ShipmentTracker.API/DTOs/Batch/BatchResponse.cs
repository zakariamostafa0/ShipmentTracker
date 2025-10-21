using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.API.DTOs.Batch;

public class BatchResponse
{
    public long Id { get; set; }
    public long BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BatchStatus Status { get; set; }
    public int ShipmentCount { get; set; }
    public decimal TotalWeight { get; set; }
    public int ThresholdCount { get; set; }
    public decimal ThresholdWeight { get; set; }
    public long? SourceWarehouseId { get; set; }
    public string? SourceWarehouseName { get; set; }
    public long? DestinationWarehouseId { get; set; }
    public string? DestinationWarehouseName { get; set; }
    public long? SourcePortId { get; set; }
    public string? SourcePortName { get; set; }
    public long? DestinationPortId { get; set; }
    public string? DestinationPortName { get; set; }
    public DateTime? CarrierAssignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
