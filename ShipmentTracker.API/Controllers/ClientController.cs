using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Announcement;
using ShipmentTracker.API.DTOs.Client;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.Shipment;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;
using System.Security.Claims;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ClientController> _logger;

    public ClientController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ClientController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet("me")]
    [Authorize(Roles = "Client")]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> GetCurrentClient()
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var client = await _unitOfWork.Clients.GetClientByUserIdAsync(userId);
            
            if (client == null)
            {
                return NotFound(ApiResponse<ClientResponse>.ErrorResult("Client profile not found"));
            }

            var clientResponse = _mapper.Map<ClientResponse>(client);
            return Ok(ApiResponse<ClientResponse>.SuccessResult(clientResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current client");
            return StatusCode(500, ApiResponse<ClientResponse>.ErrorResult("An error occurred while retrieving client information"));
        }
    }

    [HttpGet("me/shipments")]
    [Authorize(Roles = "Client")]
    public async Task<ActionResult<ApiResponse<List<ClientShipmentResponse>>>> GetMyShipments()
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var client = await _unitOfWork.Clients.GetClientByUserIdAsync(userId);
            
            if (client == null)
            {
                return NotFound(ApiResponse<List<ClientShipmentResponse>>.ErrorResult("Client profile not found"));
            }

            var shipments = await _unitOfWork.Shipments.GetShipmentsByClientAsync(client.Id);
            var shipmentResponses = _mapper.Map<List<ClientShipmentResponse>>(shipments);
            
            return Ok(ApiResponse<List<ClientShipmentResponse>>.SuccessResult(shipmentResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client shipments");
            return StatusCode(500, ApiResponse<List<ClientShipmentResponse>>.ErrorResult("An error occurred while retrieving shipments"));
        }
    }

    [HttpGet("me/shipments/{id}")]
    [Authorize(Roles = "Client")]
    public async Task<ActionResult<ApiResponse<ShipmentDetailResponse>>> GetMyShipment(long id)
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var client = await _unitOfWork.Clients.GetClientByUserIdAsync(userId);
            
            if (client == null)
            {
                return NotFound(ApiResponse<ShipmentDetailResponse>.ErrorResult("Client profile not found"));
            }

            var shipment = await _unitOfWork.Shipments.GetWithEventsAsync(id);
            if (shipment == null)
            {
                return NotFound(ApiResponse<ShipmentDetailResponse>.ErrorResult("Shipment not found"));
            }

            // Verify the shipment belongs to this client
            if (shipment.ClientId != client.Id)
            {
                return Forbid();
            }

            var shipmentResponse = _mapper.Map<ShipmentDetailResponse>(shipment);
            return Ok(ApiResponse<ShipmentDetailResponse>.SuccessResult(shipmentResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client shipment {ShipmentId}", id);
            return StatusCode(500, ApiResponse<ShipmentDetailResponse>.ErrorResult("An error occurred while retrieving shipment"));
        }
    }

    [HttpGet("me/announcements")]
    [Authorize(Roles = "Client")]
    public async Task<ActionResult<ApiResponse<List<AnnouncementResponse>>>> GetMyAnnouncements()
    {
        try
        {
            var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var client = await _unitOfWork.Clients.GetClientByUserIdAsync(userId);
            
            if (client == null)
            {
                return NotFound(ApiResponse<List<AnnouncementResponse>>.ErrorResult("Client profile not found"));
            }

            var announcements = await _unitOfWork.Announcements.GetAnnouncementsForClientAsync(client.Id);
            
            // Filter by date range
            var now = DateTime.UtcNow;
            announcements = announcements.Where(a => a.StartDate <= now && a.EndDate >= now).ToList();

            var announcementResponses = _mapper.Map<List<AnnouncementResponse>>(announcements);
            return Ok(ApiResponse<List<AnnouncementResponse>>.SuccessResult(announcementResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client announcements");
            return StatusCode(500, ApiResponse<List<AnnouncementResponse>>.ErrorResult("An error occurred while retrieving announcements"));
        }
    }

    [HttpGet("top")]
    [Authorize(Roles = "BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<List<TopClientResponse>>>> GetTopClients(
        [FromQuery] long? branchId = null,
        [FromQuery] int count = 10)
    {
        try
        {
            List<TopClientResponse> topClients;

            if (branchId.HasValue)
            {
                // Get top clients for specific branch
                var clients = await _unitOfWork.Clients.GetTopClientsByBranchAsync(branchId.Value, count);
                topClients = _mapper.Map<List<TopClientResponse>>(clients);
            }
            else
            {
                // Get top clients across all branches
                var allClients = await _unitOfWork.Clients.GetAllAsync();
                var clientsWithShipments = allClients.Where(c => c.Shipments.Any()).ToList();
                
                topClients = _mapper.Map<List<TopClientResponse>>(clientsWithShipments)
                    .OrderByDescending(c => c.ShipmentCount)
                    .Take(count)
                    .ToList();
            }

            return Ok(ApiResponse<List<TopClientResponse>>.SuccessResult(topClients));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top clients");
            return StatusCode(500, ApiResponse<List<TopClientResponse>>.ErrorResult("An error occurred while retrieving top clients"));
        }
    }
}
