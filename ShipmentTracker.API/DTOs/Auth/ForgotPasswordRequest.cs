using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Auth;

public record ForgotPasswordRequest(
    [Required]
    [EmailAddress]
    [StringLength(100)]
    string Email
);
