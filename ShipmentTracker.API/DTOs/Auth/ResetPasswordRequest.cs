using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Auth;

public record ResetPasswordRequest(
    [Required]
    [StringLength(500)]
    string Token,
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string NewPassword
);
