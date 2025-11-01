using System.ComponentModel.DataAnnotations;
using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.API.DTOs.User;

public class UpdateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; }

    public bool IsActive { get; set; } = true;
}
