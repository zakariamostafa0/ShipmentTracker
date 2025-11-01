using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.Auth;

public record NewPasswordRequest(
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string NewPassword
);
