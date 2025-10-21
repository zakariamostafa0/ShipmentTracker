using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.Role;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleController> _logger;

    public RoleController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RoleController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<RoleResponse>>>> GetRoles()
    {
        try
        {
            var roles = await _unitOfWork.Roles.GetAllAsync();
            var roleResponses = _mapper.Map<List<RoleResponse>>(roles);
            return Ok(ApiResponse<List<RoleResponse>>.SuccessResult(roleResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, ApiResponse<List<RoleResponse>>.ErrorResult("An error occurred while retrieving roles"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRole(long id)
    {
        try
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null)
            {
                return NotFound(ApiResponse<RoleResponse>.ErrorResult("Role not found"));
            }

            var roleResponse = _mapper.Map<RoleResponse>(role);
            return Ok(ApiResponse<RoleResponse>.SuccessResult(roleResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", id);
            return StatusCode(500, ApiResponse<RoleResponse>.ErrorResult("An error occurred while retrieving role"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            // Check if role name already exists
            var existingRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == request.Name);
            if (existingRole != null)
            {
                return BadRequest(ApiResponse<RoleResponse>.ErrorResult("Role name already exists"));
            }

            // Check if role type already exists
            var existingRoleType = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.RoleType == request.RoleType);
            if (existingRoleType != null)
            {
                return BadRequest(ApiResponse<RoleResponse>.ErrorResult("Role type already exists"));
            }

            var role = _mapper.Map<Role>(request);
            await _unitOfWork.Roles.AddAsync(role);
            await _unitOfWork.SaveChangesAsync();

            var roleResponse = _mapper.Map<RoleResponse>(role);
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, ApiResponse<RoleResponse>.SuccessResult(roleResponse, "Role created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return StatusCode(500, ApiResponse<RoleResponse>.ErrorResult("An error occurred while creating role"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null)
            {
                return NotFound(ApiResponse<RoleResponse>.ErrorResult("Role not found"));
            }

            // Check if role name already exists (excluding current role)
            var existingRole = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Name == request.Name && r.Id != id);
            if (existingRole != null)
            {
                return BadRequest(ApiResponse<RoleResponse>.ErrorResult("Role name already exists"));
            }

            // Check if role type already exists (excluding current role)
            var existingRoleType = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.RoleType == request.RoleType && r.Id != id);
            if (existingRoleType != null)
            {
                return BadRequest(ApiResponse<RoleResponse>.ErrorResult("Role type already exists"));
            }

            _mapper.Map(request, role);
            await _unitOfWork.Roles.UpdateAsync(role);
            await _unitOfWork.SaveChangesAsync();

            var roleResponse = _mapper.Map<RoleResponse>(role);
            return Ok(ApiResponse<RoleResponse>.SuccessResult(roleResponse, "Role updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", id);
            return StatusCode(500, ApiResponse<RoleResponse>.ErrorResult("An error occurred while updating role"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteRole(long id)
    {
        try
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);
            if (role == null)
            {
                return NotFound(ApiResponse.ErrorResult("Role not found"));
            }

            // Check if role has users assigned
            var userCount = await _unitOfWork.UserRoles.CountAsync(ur => ur.RoleId == id);
            if (userCount > 0)
            {
                return BadRequest(ApiResponse.ErrorResult($"Cannot delete role. {userCount} user(s) are assigned to this role."));
            }

            await _unitOfWork.Roles.DeleteAsync(role);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Role {RoleId} deleted successfully", id);
            return Ok(ApiResponse.SuccessResult("Role deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting role"));
        }
    }
}
