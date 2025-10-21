using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.Role;
using ShipmentTracker.API.DTOs.User;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/users/{userId}/roles")]
[Authorize]
public class UserRoleController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UserRoleController> _logger;

    public UserRoleController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UserRoleController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<RoleResponse>>>> GetUserRoles(long userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetWithRolesAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<List<RoleResponse>>.ErrorResult("User not found"));
            }

            var roles = user.UserRoles.Select(ur => ur.Role).ToList();
            var roleResponses = _mapper.Map<List<RoleResponse>>(roles);
            return Ok(ApiResponse<List<RoleResponse>>.SuccessResult(roleResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<RoleResponse>>.ErrorResult("An error occurred while retrieving user roles"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> AssignRoleToUser(long userId, [FromBody] AssignRoleRequest request)
    {
        try
        {
            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult("User not found"));
            }

            // Check if role exists
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId);
            if (role == null)
            {
                return NotFound(ApiResponse.ErrorResult("Role not found"));
            }

            // Check if user already has this role
            var existingUserRole = await _unitOfWork.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == request.RoleId);
            if (existingUserRole != null)
            {
                return BadRequest(ApiResponse.ErrorResult("User already has this role"));
            }

            // Create new user role assignment
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = request.RoleId
            };

            await _unitOfWork.UserRoles.AddAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} assigned to user {UserId}", request.RoleId, userId);
            return Ok(ApiResponse.SuccessResult("Role assigned successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, userId);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while assigning role"));
        }
    }

    [HttpDelete("{roleId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> RemoveRoleFromUser(long userId, long roleId)
    {
        try
        {
            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.ErrorResult("User not found"));
            }

            // Check if role exists
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role == null)
            {
                return NotFound(ApiResponse.ErrorResult("Role not found"));
            }

            // Find the user role assignment
            var userRole = await _unitOfWork.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole == null)
            {
                return NotFound(ApiResponse.ErrorResult("User does not have this role"));
            }

            // Check if this is the user's last role
            var userRoleCount = await _unitOfWork.UserRoles.CountAsync(ur => ur.UserId == userId);
            if (userRoleCount <= 1)
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot remove the last role from a user"));
            }

            await _unitOfWork.UserRoles.DeleteAsync(userRole);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);
            return Ok(ApiResponse.SuccessResult("Role removed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while removing role"));
        }
    }
}
