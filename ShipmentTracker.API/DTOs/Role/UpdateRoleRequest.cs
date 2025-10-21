using System.ComponentModel.DataAnnotations;
using ShipmentTracker.Core.Enums;

namespace ShipmentTracker.API.DTOs.Role;

public class UpdateRoleRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public RoleType RoleType { get; set; }
}
