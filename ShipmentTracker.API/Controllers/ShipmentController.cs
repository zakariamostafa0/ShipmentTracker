using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.Shipment;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Enums;
using ShipmentTracker.Core.Interfaces;
using System.Security.Claims;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShipmentController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ShipmentController> _logger;

    public ShipmentController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ShipmentController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "DataEntry,BranchAdmin,CarrierOperator,Admin")]
    public async Task<ActionResult<ApiResponse<List<ShipmentResponse>>>> GetShipments(
        [FromQuery] long? clientId = null,
        [FromQuery] long? batchId = null,
        [FromQuery] ShipmentStatus? status = null)
    {
        try
        {
            var shipments = await _unitOfWork.Shipments.GetAllAsync();
            
            if (clientId.HasValue)
            {
                shipments = shipments.Where(s => s.ClientId == clientId.Value);
            }
            
            if (batchId.HasValue)
            {
                shipments = shipments.Where(s => s.BatchId == batchId.Value);
            }
            
            if (status.HasValue)
            {
                shipments = shipments.Where(s => s.Status == status.Value);
            }

            var shipmentResponses = _mapper.Map<List<ShipmentResponse>>(shipments);
            return Ok(ApiResponse<List<ShipmentResponse>>.SuccessResult(shipmentResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipments");
            return StatusCode(500, ApiResponse<List<ShipmentResponse>>.ErrorResult("An error occurred while retrieving shipments"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "DataEntry,BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<ShipmentResponse>>> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        try
        {
            // Validate client exists
            var client = await _unitOfWork.Clients.GetByIdAsync(request.ClientId);
            if (client == null)
            {
                return BadRequest(ApiResponse<ShipmentResponse>.ErrorResult("Client not found"));
            }

            var shipment = _mapper.Map<Shipment>(request);
            await _unitOfWork.Shipments.AddAsync(shipment);
            await _unitOfWork.SaveChangesAsync();

            // Create initial shipment event
            var initialEvent = new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                EventType = "Created",
                Message = "Shipment created",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ShipmentEvents.AddAsync(initialEvent);
            await _unitOfWork.SaveChangesAsync();

            var shipmentResponse = _mapper.Map<ShipmentResponse>(shipment);
            return CreatedAtAction(nameof(GetShipment), new { id = shipment.Id }, 
                ApiResponse<ShipmentResponse>.SuccessResult(shipmentResponse, "Shipment created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipment");
            return StatusCode(500, ApiResponse<ShipmentResponse>.ErrorResult("An error occurred while creating shipment"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "DataEntry,BranchAdmin,CarrierOperator,Client,Admin")]
    public async Task<ActionResult<ApiResponse<ShipmentDetailResponse>>> GetShipment(long id)
    {
        try
        {
            var shipment = await _unitOfWork.Shipments.GetWithEventsAsync(id);
            if (shipment == null)
            {
                return NotFound(ApiResponse<ShipmentDetailResponse>.ErrorResult("Shipment not found"));
            }

            // Check if user is a client - they can only see their own shipments
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (userRoles.Contains("Client"))
            {
                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var client = await _unitOfWork.Clients.GetClientByUserIdAsync(userId);
                if (client == null || shipment.ClientId != client.Id)
                {
                    return Forbid();
                }
            }

            var shipmentResponse = _mapper.Map<ShipmentDetailResponse>(shipment);
            return Ok(ApiResponse<ShipmentDetailResponse>.SuccessResult(shipmentResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipment {ShipmentId}", id);
            return StatusCode(500, ApiResponse<ShipmentDetailResponse>.ErrorResult("An error occurred while retrieving shipment"));
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "CarrierOperator,Admin")]
    public async Task<ActionResult<ApiResponse>> UpdateShipmentStatus(long id, [FromBody] UpdateShipmentStatusRequest request)
    {
        try
        {
            var shipment = await _unitOfWork.Shipments.GetByIdAsync(id);
            if (shipment == null)
            {
                return NotFound(ApiResponse.ErrorResult("Shipment not found"));
            }

            var oldStatus = shipment.Status;
            shipment.Status = request.Status;
            await _unitOfWork.Shipments.UpdateAsync(shipment);

            // Create shipment event
            var shipmentEvent = new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                EventType = "StatusChanged",
                Message = request.Notes ?? $"Status changed from {oldStatus} to {request.Status}",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ShipmentEvents.AddAsync(shipmentEvent);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Shipment {ShipmentId} status updated from {OldStatus} to {NewStatus}", 
                id, oldStatus, request.Status);
            return Ok(ApiResponse.SuccessResult("Shipment status updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipment {ShipmentId} status", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while updating shipment status"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse>> CancelShipment(long id)
    {
        try
        {
            var shipment = await _unitOfWork.Shipments.GetByIdAsync(id);
            if (shipment == null)
            {
                return NotFound(ApiResponse.ErrorResult("Shipment not found"));
            }

            if (shipment.Status == ShipmentStatus.Delivered || shipment.Status == ShipmentStatus.Cancelled)
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot cancel delivered or already cancelled shipments"));
            }

            shipment.Status = ShipmentStatus.Cancelled;
            await _unitOfWork.Shipments.UpdateAsync(shipment);

            // Create cancellation event
            var cancellationEvent = new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                EventType = "Cancelled",
                Message = "Shipment cancelled",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.ShipmentEvents.AddAsync(cancellationEvent);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Shipment {ShipmentId} cancelled", id);
            return Ok(ApiResponse.SuccessResult("Shipment cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling shipment {ShipmentId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while cancelling shipment"));
        }
    }

    [HttpGet("unassigned")]
    [Authorize(Roles = "DataEntry,BranchAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<List<ShipmentResponse>>>> GetUnassignedShipments()
    {
        try
        {
            var unassignedShipments = await _unitOfWork.Shipments.GetUnassignedShipmentsAsync();
            var shipmentResponses = _mapper.Map<List<ShipmentResponse>>(unassignedShipments);
            return Ok(ApiResponse<List<ShipmentResponse>>.SuccessResult(shipmentResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unassigned shipments");
            return StatusCode(500, ApiResponse<List<ShipmentResponse>>.ErrorResult("An error occurred while retrieving unassigned shipments"));
        }
    }
}
