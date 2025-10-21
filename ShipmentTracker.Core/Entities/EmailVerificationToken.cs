namespace ShipmentTracker.Core.Entities;

public class EmailVerificationToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
