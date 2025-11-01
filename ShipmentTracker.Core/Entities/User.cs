using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.Core.Entities;

public class User : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public string DisplayName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public virtual Client? Client { get; set; }
    public virtual ICollection<ShipmentEvent> ShipmentEvents { get; set; } = new List<ShipmentEvent>();
    public virtual ICollection<Announcement> CreatedAnnouncements { get; set; } = new List<Announcement>();
    public virtual ICollection<UserPhoneNumber> PhoneNumbers { get; set; } = new List<UserPhoneNumber>();
}
