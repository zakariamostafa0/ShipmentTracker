using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.User;

public class UpdateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
