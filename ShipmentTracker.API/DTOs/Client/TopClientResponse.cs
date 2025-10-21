namespace ShipmentTracker.API.DTOs.Client;

public class TopClientResponse
{
    public long Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int ShipmentCount { get; set; }
    public decimal TotalWeight { get; set; }
    public DateTime LastShipmentDate { get; set; }
}
