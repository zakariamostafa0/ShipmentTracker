using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShipmentTracker.API.DTOs.Batch;
using ShipmentTracker.API.DTOs.Common;
using ShipmentTracker.API.DTOs.Port;
using ShipmentTracker.Core.Entities;
using ShipmentTracker.Core.Interfaces;

namespace ShipmentTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PortController> _logger;

    public PortController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PortController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "PortOperator,Admin")]
    public async Task<ActionResult<ApiResponse<List<PortResponse>>>> GetPorts([FromQuery] string? country = null)
    {
        try
        {
            var ports = await _unitOfWork.Ports.GetAllAsync();
            
            if (!string.IsNullOrEmpty(country))
            {
                ports = ports.Where(p => p.Country.Contains(country, StringComparison.OrdinalIgnoreCase));
            }

            var portResponses = _mapper.Map<List<PortResponse>>(ports);
            return Ok(ApiResponse<List<PortResponse>>.SuccessResult(portResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ports");
            return StatusCode(500, ApiResponse<List<PortResponse>>.ErrorResult("An error occurred while retrieving ports"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "PortOperator,Admin")]
    public async Task<ActionResult<ApiResponse<PortResponse>>> GetPort(long id)
    {
        try
        {
            var port = await _unitOfWork.Ports.GetByIdAsync(id);
            if (port == null)
            {
                return NotFound(ApiResponse<PortResponse>.ErrorResult("Port not found"));
            }

            var portResponse = _mapper.Map<PortResponse>(port);
            return Ok(ApiResponse<PortResponse>.SuccessResult(portResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving port {PortId}", id);
            return StatusCode(500, ApiResponse<PortResponse>.ErrorResult("An error occurred while retrieving port"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PortResponse>>> CreatePort([FromBody] CreatePortRequest request)
    {
        try
        {
            // Check if port name + country combination already exists
            var existingPort = await _unitOfWork.Ports.FirstOrDefaultAsync(p => 
                p.Name == request.Name && p.Country == request.Country);
            if (existingPort != null)
            {
                return BadRequest(ApiResponse<PortResponse>.ErrorResult("Port with this name and country already exists"));
            }

            var port = _mapper.Map<Port>(request);
            await _unitOfWork.Ports.AddAsync(port);
            await _unitOfWork.SaveChangesAsync();

            var portResponse = _mapper.Map<PortResponse>(port);
            return CreatedAtAction(nameof(GetPort), new { id = port.Id }, 
                ApiResponse<PortResponse>.SuccessResult(portResponse, "Port created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating port");
            return StatusCode(500, ApiResponse<PortResponse>.ErrorResult("An error occurred while creating port"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PortResponse>>> UpdatePort(long id, [FromBody] UpdatePortRequest request)
    {
        try
        {
            var port = await _unitOfWork.Ports.GetByIdAsync(id);
            if (port == null)
            {
                return NotFound(ApiResponse<PortResponse>.ErrorResult("Port not found"));
            }

            // Check if port name + country combination already exists (excluding current port)
            var existingPort = await _unitOfWork.Ports.FirstOrDefaultAsync(p => 
                p.Name == request.Name && p.Country == request.Country && p.Id != id);
            if (existingPort != null)
            {
                return BadRequest(ApiResponse<PortResponse>.ErrorResult("Port with this name and country already exists"));
            }

            _mapper.Map(request, port);
            await _unitOfWork.Ports.UpdateAsync(port);
            await _unitOfWork.SaveChangesAsync();

            var portResponse = _mapper.Map<PortResponse>(port);
            return Ok(ApiResponse<PortResponse>.SuccessResult(portResponse, "Port updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating port {PortId}", id);
            return StatusCode(500, ApiResponse<PortResponse>.ErrorResult("An error occurred while updating port"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> DeletePort(long id)
    {
        try
        {
            var port = await _unitOfWork.Ports.GetByIdAsync(id);
            if (port == null)
            {
                return NotFound(ApiResponse.ErrorResult("Port not found"));
            }

            // Check if port has active batches
            var activeSourceBatches = port.SourceBatches.Where(b => b.Status != Core.Enums.BatchStatus.Delivered && 
                                                                    b.Status != Core.Enums.BatchStatus.Cancelled && 
                                                                    b.Status != Core.Enums.BatchStatus.Archived);
            var activeDestinationBatches = port.DestinationBatches.Where(b => b.Status != Core.Enums.BatchStatus.Delivered && 
                                                                              b.Status != Core.Enums.BatchStatus.Cancelled && 
                                                                              b.Status != Core.Enums.BatchStatus.Archived);
            
            if (activeSourceBatches.Any() || activeDestinationBatches.Any())
            {
                return BadRequest(ApiResponse.ErrorResult("Cannot delete port with active batches"));
            }

            await _unitOfWork.Ports.DeleteAsync(port);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Port {PortId} deleted", id);
            return Ok(ApiResponse.SuccessResult("Port deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting port {PortId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("An error occurred while deleting port"));
        }
    }

    [HttpGet("{id}/batches")]
    [Authorize(Roles = "PortOperator,Admin")]
    public async Task<ActionResult<ApiResponse<List<BatchResponse>>>> GetPortBatches(long id)
    {
        try
        {
            var port = await _unitOfWork.Ports.GetByIdAsync(id);
            if (port == null)
            {
                return NotFound(ApiResponse<List<BatchResponse>>.ErrorResult("Port not found"));
            }

            // Get batches where this port is either source or destination
            var sourceBatches = port.SourceBatches.ToList();
            var destinationBatches = port.DestinationBatches.ToList();
            var allBatches = sourceBatches.Concat(destinationBatches).Distinct().ToList();

            var batchResponses = _mapper.Map<List<BatchResponse>>(allBatches);
            return Ok(ApiResponse<List<BatchResponse>>.SuccessResult(batchResponses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches for port {PortId}", id);
            return StatusCode(500, ApiResponse<List<BatchResponse>>.ErrorResult("An error occurred while retrieving port batches"));
        }
    }
}
