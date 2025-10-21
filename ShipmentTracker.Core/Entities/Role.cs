using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.Core.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public RoleType RoleType { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
