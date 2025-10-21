using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Announcement;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;
using System.Security.Claims;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnnouncementController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AnnouncementController> _logger;

    public AnnouncementController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AnnouncementController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AnnouncementResponse>>>> GetAnnouncements()
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            
            List<Announcement> announcements;

            if (userRoles.Contains("Admin"))
            {
                // Admin can see all announcements
                announcements = (await _unitOfWork.Announcements.GetAllAsync()).ToList();
            }
            else if (userRoles.Contains("Client"))
            {
                // Client can see announcements targeted to them
                var client = await _unitOfWork.Clients.GetClientByUserIdAsync(userId);
                if (client == null)
                {
                    return BadRequest(ApiResponse<List<AnnouncementResponse>>.ErrorResult("Client profile not found"));
                }
                announcements = (await _unitOfWork.Announcements.GetAnnouncementsForClientAsync(client.Id)).ToList();
            }
            else
            {
                // Other roles can see active announcements
                announcements = (await _unitOfWork.Announcements.GetActiveAnnouncementsAsync()).ToList();
            }

            // Filter by date range
            var now = DateTime.UtcNow;
            announcements = announcements.Where(a => a.StartDate <= now && a.EndDate >= now).ToList();

            var announcementResponses = _mapper.Map<List<AnnouncementResponse>>(announcements);
            return Ok(ApiResponse<List<AnnouncementResponse>>.SuccessResult(announcementResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving announcements");
            return StatusCode(500, ApiResponse<List<AnnouncementResponse>>.ErrorResult("An error occurred while retrieving announcements"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AnnouncementResponse>>> CreateAnnouncement([FromBody] CreateAnnouncementRequest request)
    {
        try
        {
            if (request.StartDate >= request.EndDate)
            {
                return BadRequest(ApiResponse<AnnouncementResponse>.ErrorResult("Start date must be before end date"));
            }

            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var announcement = _mapper.Map<Announcement>(request);
            announcement.CreatedByUserId = userId;
            
            await _unitOfWork.Announcements.AddAsync(announcement);
            await _unitOfWork.SaveChangesAsync();

            // Create announcement targets
            foreach (var targetDto in request.Targets)
            {
                var target = _mapper.Map<AnnouncementTarget>(targetDto);
                target.AnnouncementId = announcement.Id;
                await _unitOfWork.AnnouncementTargets.AddAsync(target);
            }
            
            await _unitOfWork.SaveChangesAsync();

            var announcementResponse = _mapper.Map<AnnouncementResponse>(announcement);
            return CreatedAtAction(nameof(GetAnnouncement), new { id = announcement.Id }, 
                ApiResponse<AnnouncementResponse>.SuccessResult(announcementResponse, "Announcement created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating announcement");
            return StatusCode(500, ApiResponse<AnnouncementResponse>.ErrorResult("An error occurred while creating announcement"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AnnouncementResponse>>> GetAnnouncement(long id)
    {
        try
        {
            var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);
            if (announcement == null)
            {
                return NotFound(ApiResponse<AnnouncementResponse>.ErrorResult("Announcement not found"));
            }

            // Check if announcement is active
            var now = DateTime.UtcNow;
            if (announcement.StartDate > now || announcement.EndDate < now)
            {
                return NotFound(ApiResponse<AnnouncementResponse>.ErrorResult("Announcement not found or not active"));
            }

            var announcementResponse = _mapper.Map<AnnouncementResponse>(announcement);
            return Ok(ApiResponse<AnnouncementResponse>.SuccessResult(announcementResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving announcement {AnnouncementId}", id);
            return StatusCode(500, ApiResponse<AnnouncementResponse>.ErrorResult("An error occurred while retrieving announcement"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AnnouncementResponse>>> UpdateAnnouncement(long id, [FromBody] UpdateAnnouncementRequest request)
    {
        try
        {
            if (request.StartDate >= request.EndDate)
            {
                return BadRequest(ApiResponse<AnnouncementResponse>.ErrorResult("Start date must be before end date"));
            }

            var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);
            if (announcement == null)
            {
                return NotFound(ApiResponse<AnnouncementResponse>.ErrorResult("Announcement not found"));
            }

            _mapper.Map(request, announcement);
            await _unitOfWork.Announcements.UpdateAsync(announcement);
            await _unitOfWork.SaveChangesAsync();

            var announcementResponse = _mapper.Map<AnnouncementResponse>(announcement);
            return Ok(ApiResponse<AnnouncementResponse>.SuccessResult(announcementResponse, "Announcement updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating announcement {AnnouncementId}", id);
            return StatusCode(500, ApiResponse<AnnouncementResponse>.ErrorResult("An error occurred while updating announcement"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteAnnouncement(long id)
    {
        try
        {
            var announcement = await _unitOfWork.Announcements.GetByIdAsync(id);
            if (announcement == null)
            {
                return NotFound(ApiResponse.ErrorResult("Announcement not found"));
            }

            // Delete announcement targets first
            var targets = await _unitOfWork.AnnouncementTargets.GetAllAsync();
            var announcementTargets = targets.Where(t => t.AnnouncementId == id).ToList();
            
            foreach (var target in announcementTargets)
            {
                await _unitOfWork.AnnouncementTargets.DeleteAsync(target);
            }

            await _unitOfWork.Announcements.DeleteAsync(announcement);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Announcement {AnnouncementId} deleted", id);
            return Ok(ApiResponse.SuccessResult("Announcement deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting announcement {AnnouncementId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting announcement"));
        }
    }
}
