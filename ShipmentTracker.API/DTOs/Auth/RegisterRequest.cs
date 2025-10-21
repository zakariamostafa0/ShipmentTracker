using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Auth;

public record RegisterRequest(
    [Required]
    [StringLength(50, MinimumLength = 3)]
    string UserName,
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    string Email,
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string DisplayName,
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string Password,
    
    [StringLength(20)]
    string? PhoneNumber
);
