using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.API.DTOs.Role;

public class RoleResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoleType RoleType { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
