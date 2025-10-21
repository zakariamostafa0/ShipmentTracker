using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Auth;

public record VerifyEmailRequest(
    [Required]
    [StringLength(500)]
    string Token
);
