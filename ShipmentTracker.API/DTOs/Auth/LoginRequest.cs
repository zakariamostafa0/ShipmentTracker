using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Auth;

public record LoginRequest(
    [Required]
    [StringLength(50)]
    string UserName,
    
    [Required]
    [StringLength(100)]
    string Password
);
