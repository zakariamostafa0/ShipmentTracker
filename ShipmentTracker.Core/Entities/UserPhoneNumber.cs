namespace ShipmentTracker.Core.Entities;

public class UserPhoneNumber : BaseEntity
{
    public long UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;
    
    // Navigation property
    public virtual User User { get; set; } = null!;
}
