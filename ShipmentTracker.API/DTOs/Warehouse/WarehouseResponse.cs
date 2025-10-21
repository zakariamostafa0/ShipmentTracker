namespace ShipmentTracker.API.DTOs.Warehouse;

public class WarehouseResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int SourceBatchCount { get; set; }
    public int DestinationBatchCount { get; set; }
    public int ActiveBatchCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
