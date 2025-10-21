using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Auth;

public record RefreshTokenRequest(
    [Required]
    [StringLength(1000)]
    string AccessToken,
    
    [Required]
    [StringLength(500)]
    string RefreshToken
);
