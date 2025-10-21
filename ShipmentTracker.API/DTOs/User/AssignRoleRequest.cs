using System.ComponentModel.DataAnnotations;

namespace ShipmentTracker.API.DTOs.User;

public class AssignRoleRequest
{
    [Required]
    public long RoleId { get; set; }
}
