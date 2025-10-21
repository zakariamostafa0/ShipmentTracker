using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.Core.Entities;

public class Batch : BaseEntity
{
    public long BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Draft;
    public int ShipmentCount { get; set; } = 0;
    public decimal TotalWeight { get; set; } = 0;
    public int ThresholdCount { get; set; }
    public decimal ThresholdWeight { get; set; }
    public long? SourceWarehouseId { get; set; }
    public long? DestinationWarehouseId { get; set; }
    public long? SourcePortId { get; set; }
    public long? DestinationPortId { get; set; }
    public DateTime? CarrierAssignedAt { get; set; }
    
    // Navigation properties
    public virtual Branch Branch { get; set; } = null!;
    public virtual Warehouse? SourceWarehouse { get; set; }
    public virtual Warehouse? DestinationWarehouse { get; set; }
    public virtual Port? SourcePort { get; set; }
    public virtual Port? DestinationPort { get; set; }
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
